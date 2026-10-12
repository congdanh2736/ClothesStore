using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] hằng trạng thái, BusinessException, OrderPricing, LoyaltyRules
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.Order;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

/*
 * [THÊM] Ghi chú nghiệp vụ - Vòng đời đơn hàng:
 *   Pending ──confirm──> Confirmed ──(tạo Shipment)──> Shipping ──(Shipment Delivered)──> Completed ──(trả hàng)──> Returned
 *      │                    │  └──────────(khách lấy tại cửa hàng: complete)──────────────────┘
 *      └──cancel──> Cancelled <──cancel──┘   (Shipping chỉ được hủy khi Shipment = Failed)
 *
 * Các phụ thuộc nghiệp vụ được xử lý trong 1 transaction:
 *   - Đặt đơn   : kiểm tra & trừ Store_Stock, áp dụng Promotion, tạo Payment_Transaction (Pending), Store_Daily_Stat.TotalOrders
 *   - Hủy đơn   : hoàn Store_Stock, Payment -> Cancelled/Refunded, Store_Daily_Stat.CanceledOrders
 *   - Hoàn tất  : Payment COD -> Paid, cộng Loyalty_Transaction + xét hạng, Store_Daily_Stat / Store_Item_Stat
 */
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IInventoryService _inventory;             // [THÊM] kiểm tra / trừ / hoàn tồn kho
    private readonly IStoreStatisticsService _statistics;      // [THÊM] cập nhật thống kê cửa hàng
    private readonly ILoyaltyTransactionService _loyalty;      // [THÊM] cộng điểm khi hoàn tất đơn
    private readonly ICartRepository _cartRepository;          // [THÊM] đặt hàng từ giỏ (checkout)

    // [SỬA] thêm 4 tham số inventory, statistics, loyalty, cartRepository
    public OrderService(
        IOrderRepository repository,
        ApplicationDbContext context,
        IMapper mapper,
        IInventoryService inventory,
        IStoreStatisticsService statistics,
        ILoyaltyTransactionService loyalty,
        ICartRepository cartRepository)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
        _inventory = inventory;           // [THÊM]
        _statistics = statistics;         // [THÊM]
        _loyalty = loyalty;               // [THÊM]
        _cartRepository = cartRepository; // [THÊM]
    }

    public async Task<IEnumerable<OrderDetailDTO>> GetAllAsync()
        => _mapper.Map<IEnumerable<OrderDetailDTO>>(await _repository.GetAllAsync());

    // [THÊM] Lịch sử đơn hàng của 1 khách
    public async Task<IEnumerable<OrderDetailDTO>> GetByCustomerIdAsync(int customerId)
        => _mapper.Map<IEnumerable<OrderDetailDTO>>(await _repository.GetByCustomerIdAsync(customerId));

    public async Task<OrderDetailDTO?> GetByIdAsync(int id)
    {
        var order = await _repository.GetByIdWithDetailsAsync(id);
        return order is null ? null : _mapper.Map<OrderDetailDTO>(order);
    }

    // ------------------------------------------------------------------ TẠO ĐƠN

    // Tạo đơn trực tiếp (nhân viên tạo đơn tại quầy / test API)
    // [SỬA] Viết lại: dùng chung PlaceOrderAsync với checkout -> có trừ kho, áp khuyến mãi, thống kê, chạy trong transaction.
    //       Trước đây: không trừ kho, PromotionId bắt buộc, không trừ tiền giảm giá, map CreateOrderRequest -> Order bằng AutoMapper.
    public async Task<(bool Success, string? Error, OrderDetailDTO? Data)> CreateAsync(CreateOrderRequest request)
    {
        try
        {
            // [THÊM] Chuyển danh sách sản phẩm sang StockLine để kiểm tra / trừ kho
            var lines = (request.Items ?? new())
                .Select(item => new StockLine(item.VariantId, item.Quantity))
                .ToList();

            // [SỬA] Khuyến mãi không bắt buộc: chỉ kiểm tra khi có PromotionId
            var promotion = request.PromotionId.HasValue
                ? await GetActivePromotionAsync(p => p.Id == request.PromotionId.Value)
                : null;

            // [THÊM] Tạo đơn trong 1 transaction (lỗi giữa chừng thì không mất kho / không tạo đơn dở dang)
            var orderId = await _context.InTransactionAsync(() => PlaceOrderAsync(
                request.CustomerId, request.AddressId, promotion, request.StoreId, request.PaymentMethod, lines));

            return (true, null, await GetByIdAsync(orderId));
        }
        catch (BusinessException ex)
        {
            return (false, ex.Message, null);
        }
    }

    // [THÊM] Đặt hàng từ giỏ hàng (luồng chính của khách - trước đây chưa có)
    public async Task<OrderDetailDTO> CheckoutAsync(CheckoutRequest request)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(request.CustomerId);
        if (cart is null || cart.CartItems is null || cart.CartItems.Count == 0)
            throw new BusinessException("Giỏ hàng đang trống, không thể đặt hàng.");

        Promotion? promotion = null;
        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var code = request.PromotionCode.Trim();
            promotion = await GetActivePromotionAsync(p => p.Code == code);
        }

        var lines = cart.CartItems
            .Select(ci => new StockLine(ci.VariantId, ci.Quantity))
            .ToList();

        var orderId = await _context.InTransactionAsync(async () =>
        {
            var id = await PlaceOrderAsync(
                request.CustomerId, request.AddressId, promotion, request.StoreId, request.PaymentMethod, lines);

            // Đặt hàng thành công -> làm trống giỏ hàng
            await _cartRepository.ClearCartAsync(cart.Id);
            return id;
        });

        return (await GetByIdAsync(orderId))!;
    }

    // [THÊM] Logic tạo đơn dùng chung cho CreateAsync và CheckoutAsync
    private async Task<int> PlaceOrderAsync(
        int customerId,
        int addressId,
        Promotion? promotion,
        int? storeId,
        string? paymentMethod,
        List<StockLine> lines)
    {
        await ValidateCustomerAndAddressAsync(customerId, addressId);

        if (lines.Count == 0 || lines.Any(l => l.Quantity <= 0))
            throw new BusinessException("Đơn hàng phải có ít nhất một sản phẩm với số lượng hợp lệ.");

        var items = await BuildItemsAsync(lines);
        var method = NormalizePaymentMethod(paymentMethod);

        // [THÊM] Chọn / kiểm tra cửa hàng xuất kho
        var fulfillStoreId = await _inventory.ResolveStoreAsync(storeId, lines);

        var order = new Order
        {
            CustomerId = customerId,
            AddressId = addressId,
            PromotionId = promotion?.Id,
            Promotion = promotion,
            StoreId = fulfillStoreId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderItems = items,
            PaymentTransaction = new PaymentTransaction
            {
                PaymentMethod = method,
                Status = PaymentStatus.Pending
            }
        };
        // [SỬA] Tính tiền có trừ khuyến mãi (trước đây TotalAmount = tổng tiền hàng, bỏ qua Promotion)
        OrderPricing.Recalculate(order);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _inventory.DecreaseStockAsync(fulfillStoreId, lines); // [THÊM] trừ tồn kho
        await _statistics.RecordOrderPlacedAsync(order);              // [THÊM] Store_Daily_Stat.TotalOrders + 1

        return order.Id;
    }

    // ------------------------------------------------------------------ SỬA / XÓA ĐƠN

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateOrderRequest request)
    {
        var order = await _repository.GetByIdWithDetailsAsync(id);
        if (order is null)
            return (false, "Không tìm thấy đơn hàng.");

        // [THÊM] Chỉ sửa được đơn Pending (trước đây sửa được cả đơn đã giao / đã hủy)
        if (order.Status != OrderStatus.Pending)
            return (false, "Chỉ được sửa đơn hàng khi đơn đang ở trạng thái chờ xác nhận (Pending).");

        try
        {
            await _context.InTransactionAsync(async () =>
            {
                await ValidateCustomerAndAddressAsync(order.CustomerId, request.AddressId);
                order.AddressId = request.AddressId;

                // [SỬA] Khuyến mãi không bắt buộc; chỉ kiểm tra hiệu lực khi đổi mã
                if (request.PromotionId != order.PromotionId)
                {
                    order.Promotion = request.PromotionId.HasValue
                        ? await GetActivePromotionAsync(p => p.Id == request.PromotionId.Value)
                        : null;
                    order.PromotionId = request.PromotionId;
                }

                if (request.Items is not null)
                {
                    // [SỬA] chuyển sang StockLine + kiểm tra danh sách rỗng
                    var newLines = request.Items.Select(i => new StockLine(i.VariantId, i.Quantity)).ToList();
                    if (newLines.Count == 0)
                        throw new BusinessException("Đơn hàng phải có ít nhất một sản phẩm.");

                    var oldLines = order.OrderItems.Select(i => new StockLine(i.VariantId, i.Quantity)).ToList();
                    var newItems = await BuildItemsAsync(newLines);

                    // [THÊM] Hoàn kho các dòng cũ rồi kiểm tra & trừ kho theo dòng mới (cùng cửa hàng)
                    if (order.StoreId.HasValue)
                        await _inventory.IncreaseStockAsync(order.StoreId.Value, oldLines);

                    var storeId = await _inventory.ResolveStoreAsync(order.StoreId, newLines);
                    await _inventory.DecreaseStockAsync(storeId, newLines);
                    order.StoreId = storeId;

                    // (giữ như cũ) xóa các dòng cũ, thay bằng dòng mới
                    _context.OrderItems.RemoveRange(order.OrderItems);
                    order.OrderItems = newItems; // [SỬA] TotalAmount không gán ở đây nữa mà tính bằng OrderPricing bên dưới
                }

                // [SỬA] tính lại tiền có trừ khuyến mãi
                OrderPricing.Recalculate(order);
                await _context.SaveChangesAsync();
            });
        }
        catch (BusinessException ex) // [THÊM] lỗi nghiệp vụ (hết hàng, mã hết hạn...) trả về dạng (false, message)
        {
            return (false, ex.Message);
        }

        return (true, null);
    }

    // [SỬA] Chỉ xóa được đơn đã hủy (đơn đang xử lý phải hủy trước để hoàn kho).
    //       Trước đây xóa được mọi đơn -> mất tồn kho và lịch sử doanh thu.
    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _repository.GetByIdAsync(id);
        if (order is null)
            return false;

        // [THÊM] chặn xóa đơn chưa hủy
        if (order.Status != OrderStatus.Cancelled)
            throw new BusinessException("Chỉ được xóa đơn hàng đã hủy. Hãy hủy đơn trước khi xóa.");

        await _repository.DeleteAsync(order);
        return true;
    }

    // ------------------------------------------------------------------ CHUYỂN TRẠNG THÁI

    // [THÊM] Pending -> Confirmed
    public async Task<OrderDetailDTO> ConfirmAsync(int id)
    {
        var order = await GetOrderOrThrowAsync(id);

        if (order.Status != OrderStatus.Pending)
            throw new BusinessException($"Chỉ xác nhận được đơn đang chờ (Pending). Trạng thái hiện tại: {order.Status}.");

        // Thanh toán online phải được ghi nhận thành công trước khi xác nhận đơn
        var payment = order.PaymentTransaction;
        if (payment is not null && !IsPayOnReceive(payment.PaymentMethod) && payment.Status != PaymentStatus.Paid)
            throw new BusinessException("Đơn thanh toán online chưa được thanh toán, chưa thể xác nhận.");

        order.Status = OrderStatus.Confirmed;
        await _context.SaveChangesAsync();

        return _mapper.Map<OrderDetailDTO>(order);
    }

    // [THÊM] Hủy đơn: hoàn kho, hủy/hoàn tiền thanh toán, thống kê CanceledOrders
    public async Task<OrderDetailDTO> CancelAsync(int id)
    {
        var order = await GetOrderOrThrowAsync(id);

        var canCancel = order.Status == OrderStatus.Pending
            || order.Status == OrderStatus.Confirmed
            || (order.Status == OrderStatus.Shipping && order.Shipment?.Status == ShipmentStatus.Failed);

        if (!canCancel)
            throw new BusinessException($"Không thể hủy đơn ở trạng thái {order.Status}.");

        await _context.InTransactionAsync(async () =>
        {
            // Hoàn lại tồn kho cho cửa hàng đã xuất
            if (order.StoreId.HasValue)
            {
                await _inventory.IncreaseStockAsync(order.StoreId.Value,
                    order.OrderItems.Select(i => new StockLine(i.VariantId, i.Quantity)));
            }

            if (order.PaymentTransaction is not null)
            {
                order.PaymentTransaction.Status = order.PaymentTransaction.Status == PaymentStatus.Paid
                    ? PaymentStatus.Refunded
                    : PaymentStatus.Cancelled;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            await _statistics.RecordOrderCanceledAsync(order);
        });

        return _mapper.Map<OrderDetailDTO>(order);
    }

    // [THÊM] Hoàn tất đơn: thanh toán COD -> Paid, cộng điểm + xét hạng, thống kê doanh thu
    public async Task<OrderDetailDTO> CompleteAsync(int id)
    {
        var order = await GetOrderOrThrowAsync(id);

        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Shipping)
            throw new BusinessException($"Chỉ hoàn tất được đơn đã xác nhận hoặc đang giao. Trạng thái hiện tại: {order.Status}.");

        // Đơn đang giao thì phải hoàn tất qua Shipment (Delivered)
        if (order.Shipment is not null && order.Shipment.Status != ShipmentStatus.Delivered)
            throw new BusinessException("Đơn đang được vận chuyển, hãy cập nhật trạng thái vận chuyển thành Delivered.");

        await _context.InTransactionAsync(async () =>
        {
            // COD / tiền mặt: khách trả tiền khi nhận hàng
            var payment = order.PaymentTransaction;
            if (payment is not null && payment.Status != PaymentStatus.Paid)
            {
                if (!IsPayOnReceive(payment.PaymentMethod))
                    throw new BusinessException("Đơn chưa được thanh toán, không thể hoàn tất.");

                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;
            }

            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            // Tích điểm cho khách + xét hạng thành viên
            var points = LoyaltyRules.CalculatePoints(order.TotalAmount);
            await _loyalty.AddPointsAsync(order.CustomerId, points, $"Tích điểm đơn hàng #{order.Id}");

            await _statistics.RecordOrderCompletedAsync(order);
        });

        return _mapper.Map<OrderDetailDTO>(order);
    }

    // ------------------------------------------------------------------ HÀM HỖ TRỢ

    // [THÊM]
    private async Task<Order> GetOrderOrThrowAsync(int id)
        => await _repository.GetByIdWithDetailsAsync(id)
            ?? throw new NotFoundException("Không tìm thấy đơn hàng.");

    // [SỬA] Tách từ ValidateOrderRequestAsync cũ: chỉ còn kiểm tra khách + địa chỉ, ném BusinessException thay vì trả string.
    //       Phần kiểm tra Promotion chuyển sang GetActivePromotionAsync (khuyến mãi không còn bắt buộc).
    private async Task ValidateCustomerAndAddressAsync(int customerId, int addressId)
    {
        if (!await _context.Customers.AnyAsync(customer => customer.Id == customerId))
            throw new NotFoundException("Không tìm thấy khách hàng."); // [SỬA] return string -> throw

        if (!await _context.Addresses.AnyAsync(address => address.Id == addressId && address.CustomerId == customerId))
            throw new BusinessException("Địa chỉ không thuộc về khách hàng này."); // [SỬA] return string -> throw
    }

    // [THÊM] Tìm khuyến mãi theo Id hoặc theo Code và kiểm tra còn hiệu lực
    private async Task<Promotion> GetActivePromotionAsync(System.Linq.Expressions.Expression<Func<Promotion, bool>> predicate)
    {
        var promotion = await _context.Promotions.FirstOrDefaultAsync(predicate)
            ?? throw new BusinessException("Mã khuyến mãi không tồn tại.");

        if (!OrderPricing.IsPromotionActive(promotion, DateTime.UtcNow))
            throw new BusinessException("Mã khuyến mãi đã hết hạn hoặc chưa đến thời gian áp dụng.");

        return promotion;
    }

    // [SỬA] Nhận StockLine thay vì CreateOrderItemRequest; kiểm tra biến thể tồn tại; gộp dòng trùng biến thể; giá đã là decimal
    private async Task<List<OrderItem>> BuildItemsAsync(IEnumerable<StockLine> lines)
    {
        var lineList = lines.ToList();
        var variantIds = lineList.Select(item => item.VariantId).Distinct().ToList();
        var variants = await _context.ProductVariants
            .Where(variant => variantIds.Contains(variant.VariantId))
            .ToDictionaryAsync(variant => variant.VariantId);

        // [THÊM] kiểm tra biến thể tồn tại ngay tại đây (trước đây variants[id] có thể ném KeyNotFoundException -> 500)
        if (variants.Count != variantIds.Count)
            throw new BusinessException("Phiên bản sản phẩm không tồn tại.");

        // [SỬA] Gộp các dòng trùng biến thể, giá lấy theo giá hiện tại của biến thể (chốt giá tại thời điểm đặt)
        return lineList
            .GroupBy(item => item.VariantId)
            .Select(group => new OrderItem
            {
                VariantId = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                Price = variants[group.Key].Price // [SỬA] bỏ ép kiểu (decimal) vì Price đã là decimal
            })
            .ToList();
    }

    // [THÊM] Chuẩn hóa / kiểm tra phương thức thanh toán (trước đây nhận chuỗi bất kỳ)
    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return PaymentMethods.Cod;

        var match = PaymentMethods.All.FirstOrDefault(m =>
            string.Equals(m, paymentMethod.Trim(), StringComparison.OrdinalIgnoreCase));

        return match ?? throw new BusinessException(
            $"Phương thức thanh toán không hợp lệ. Hỗ trợ: {string.Join(", ", PaymentMethods.All)}.");
    }

    // [THÊM] Các phương thức trả tiền khi nhận hàng
    private static bool IsPayOnReceive(string paymentMethod)
        => string.Equals(paymentMethod, PaymentMethods.Cod, StringComparison.OrdinalIgnoreCase)
           || string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase);
}
