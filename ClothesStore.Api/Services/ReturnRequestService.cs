// [THÊM MỚI] File mới - toàn bộ nội dung file này là phần thêm.
// Mục đích: Service đổi/trả hàng: tạo yêu cầu, duyệt, từ chối, hoàn tất (hoàn kho, trừ điểm, hoàn tiền)
using AutoMapper;
using ClothesStore.Api.Common;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.ReturnRequest;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Services
{
    /*
     * Đổi / trả hàng (Return_Request, Return_Item):
     *   Pending ──approve──> Approved ──complete (đã nhận lại hàng)──> Completed
     *      └──reject──> Rejected
     * Khi Completed:
     *   - Hoàn Store_Stock về cửa hàng đã xuất kho
     *   - Store_Item_Stat.ReturnQuantity
     *   - Trừ Loyalty_Transaction tương ứng số tiền hoàn + xét lại hạng
     *   - Payment_Transaction -> Refunded / PartiallyRefunded, Order -> Returned nếu trả toàn bộ
     */
    public class ReturnRequestService : IReturnRequestService
    {
        // Số ngày tối đa được yêu cầu đổi/trả kể từ ngày đặt hàng
        private const int ReturnWindowDays = 30;

        private readonly IReturnRequestRepository _repository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IInventoryService _inventory;
        private readonly IStoreStatisticsService _statistics;
        private readonly ILoyaltyTransactionService _loyalty;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ReturnRequestService(
            IReturnRequestRepository repository,
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IInventoryService inventory,
            IStoreStatisticsService statistics,
            ILoyaltyTransactionService loyalty,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _repository = repository;
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _inventory = inventory;
            _statistics = statistics;
            _loyalty = loyalty;
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetAllAsync()
            => _mapper.Map<IEnumerable<ReturnRequestDto>>(await _repository.GetAllAsync());

        public async Task<ReturnRequestDto?> GetByIdAsync(int id, string userId = "", bool isStaff = true)
        {
            var request = await _repository.GetByIdWithDetailsAsync(id);
            if (request is null) return null;

            if (!isStaff)
            {
                var customer = await GetCustomerByUserIdOrThrowAsync(userId);
                if (request.Order == null || request.Order.CustomerId != customer.Id)
                    throw new ForbiddenException("Bạn không có quyền xem yêu cầu trả hàng này.");
            }

            return _mapper.Map<ReturnRequestDto>(request);
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetByOrderIdAsync(int orderId, string userId = "", bool isStaff = true)
        {
            if (isStaff)
                return _mapper.Map<IEnumerable<ReturnRequestDto>>(await _repository.GetByOrderIdAsync(orderId));

            var customer = await GetCustomerByUserIdOrThrowAsync(userId);
            var order = await _orderRepository.GetByIdAsync(orderId)
                ?? throw new NotFoundException("Không tìm thấy đơn hàng.");

            if (order.CustomerId != customer.Id)
                throw new ForbiddenException("Bạn không có quyền xem yêu cầu trả hàng của đơn hàng này.");

            return _mapper.Map<IEnumerable<ReturnRequestDto>>(
                await _repository.GetByOrderIdAndCustomerIdAsync(orderId, customer.Id));
        }

        public async Task<IEnumerable<ReturnRequestDto>> GetByCustomerIdAsync(int customerId, string userId = "", bool isStaff = true)
        {
            if (isStaff)
                return _mapper.Map<IEnumerable<ReturnRequestDto>>(await _repository.GetByCustomerIdAsync(customerId));

            var customer = await GetCustomerByUserIdOrThrowAsync(userId);
            if (customerId != customer.Id)
                throw new ForbiddenException("Bạn không có quyền xem yêu cầu trả hàng của khách hàng khác.");

            return _mapper.Map<IEnumerable<ReturnRequestDto>>(await _repository.GetByCustomerIdAsync(customer.Id));
        }

        public async Task<ReturnRequestDto> CreateAsync(CreateReturnRequestDto dto, string userId)
        {
            var customer = await GetCustomerByUserIdOrThrowAsync(userId);

            var order = await _orderRepository.GetByIdWithDetailsAsync(dto.OrderId)
                ?? throw new NotFoundException("Không tìm thấy đơn hàng.");

            if (order.CustomerId != customer.Id)
                throw new ForbiddenException("Bạn không có quyền yêu cầu trả hàng cho đơn hàng này.");

            if (order.Status != OrderStatus.Completed)
                throw new BusinessException("Chỉ được yêu cầu đổi/trả với đơn hàng đã giao thành công (Completed).");

            if (order.OrderDate.AddDays(ReturnWindowDays) < DateTime.UtcNow)
                throw new BusinessException($"Đơn hàng đã quá thời hạn đổi/trả ({ReturnWindowDays} ngày).");

            // Số lượng đã/đang trả của từng dòng (không tính yêu cầu bị từ chối)
            var returned = await _repository.GetReturnedQuantitiesAsync(order.Id);

            // Tỉ lệ thực trả sau khuyến mãi để tính tiền hoàn cho đúng
            var subtotal = order.TotalAmount + order.DiscountAmount;
            var payRatio = subtotal == 0 ? 0m : order.TotalAmount / subtotal;

            var returnRequest = new ReturnRequest
            {
                OrderId = order.Id,
                Reason = dto.Reason.Trim(),
                RequestDate = DateTime.UtcNow,
                Status = ReturnStatus.Pending
            };

            foreach (var line in dto.Items)
            {
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == line.OrderItemId)
                    ?? throw new BusinessException($"Sản phẩm #{line.OrderItemId} không thuộc đơn hàng này.");

                var remaining = orderItem.Quantity - returned.GetValueOrDefault(orderItem.Id);
                if (line.Quantity > remaining)
                    throw new BusinessException(
                        $"Sản phẩm #{line.OrderItemId} chỉ còn có thể trả tối đa {remaining} chiếc.");

                returnRequest.ReturnItems.Add(new ReturnItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = line.Quantity,
                    Condition = line.Condition?.Trim(),
                    RefundAmount = Math.Round(orderItem.Price * line.Quantity * payRatio, 2)
                });
            }

            await _repository.AddAsync(returnRequest);
            return (await GetByIdAsync(returnRequest.Id, userId, isStaff: true))!;
        }

        public async Task<ReturnRequestDto> ApproveAsync(int id)
        {
            var request = await GetOrThrowAsync(id);
            if (request.Status != ReturnStatus.Pending)
                throw new BusinessException("Chỉ duyệt được yêu cầu đang chờ xử lý.");

            request.Status = ReturnStatus.Approved;
            await _repository.UpdateAsync(request);
            return _mapper.Map<ReturnRequestDto>(request);
        }

        public async Task<ReturnRequestDto> RejectAsync(int id)
        {
            var request = await GetOrThrowAsync(id);
            if (request.Status != ReturnStatus.Pending && request.Status != ReturnStatus.Approved)
                throw new BusinessException("Chỉ từ chối được yêu cầu đang chờ xử lý hoặc đã duyệt.");

            request.Status = ReturnStatus.Rejected;
            await _repository.UpdateAsync(request);
            return _mapper.Map<ReturnRequestDto>(request);
        }

        public async Task<ReturnRequestDto> CompleteAsync(int id)
        {
            var request = await GetOrThrowAsync(id);
            if (request.Status != ReturnStatus.Approved)
                throw new BusinessException("Chỉ hoàn tất được yêu cầu đã được duyệt (Approved).");

            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId)
                ?? throw new NotFoundException("Không tìm thấy đơn hàng.");

            var lines = request.ReturnItems
                .Select(ri => new StockLine(ri.OrderItem!.VariantId, ri.Quantity))
                .ToList();
            var totalRefund = request.ReturnItems.Sum(ri => ri.RefundAmount);

            await _context.InTransactionAsync(async () =>
            {
                request.Status = ReturnStatus.Completed;
                await _repository.UpdateAsync(request);

                // Nhập lại kho + thống kê trả hàng
                if (order.StoreId.HasValue)
                {
                    await _inventory.IncreaseStockAsync(order.StoreId.Value, lines);
                    await _statistics.RecordReturnAsync(order.StoreId.Value, lines);
                }

                // Trừ điểm đã tích tương ứng với số tiền được hoàn
                var points = LoyaltyRules.CalculatePoints(totalRefund);
                await _loyalty.AddPointsAsync(order.CustomerId, -points, $"Trừ điểm do trả hàng (yêu cầu #{request.Id}, đơn #{order.Id})");

                // Đã trả hết toàn bộ sản phẩm của đơn?
                var completedReturns = (await _repository.GetByOrderIdAsync(order.Id))
                    .Where(r => r.Status == ReturnStatus.Completed)
                    .SelectMany(r => r.ReturnItems)
                    .GroupBy(ri => ri.OrderItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(ri => ri.Quantity));

                var fullyReturned = order.OrderItems.All(oi => completedReturns.GetValueOrDefault(oi.Id) >= oi.Quantity);

                if (order.PaymentTransaction is not null)
                    order.PaymentTransaction.Status = fullyReturned ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

                if (fullyReturned)
                    order.Status = OrderStatus.Returned;

                await _context.SaveChangesAsync();
            });

            return (await GetByIdAsync(id, "", isStaff: true))!;
        }

        public async Task<bool> DeleteAsync(int id, string userId = "", bool isStaff = true)
        {
            var request = await _repository.GetByIdWithDetailsAsync(id);
            if (request is null) return false;

            if (!isStaff)
            {
                var customer = await GetCustomerByUserIdOrThrowAsync(userId);
                if (request.Order == null || request.Order.CustomerId != customer.Id)
                    throw new ForbiddenException("Bạn không có quyền xóa yêu cầu trả hàng này.");
            }

            if (request.Status != ReturnStatus.Pending && request.Status != ReturnStatus.Rejected)
                throw new BusinessException("Chỉ được xóa yêu cầu đang chờ xử lý hoặc đã bị từ chối.");

            await _repository.DeleteAsync(request);
            return true;
        }

        private async Task<Customer> GetCustomerByUserIdOrThrowAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ForbiddenException("Không tìm thấy thông tin xác thực tài khoản.");

            return await _customerRepository.GetByApplicationUserIdAsync(userId)
                ?? throw new ForbiddenException("Tài khoản chưa được liên kết với hồ sơ khách hàng.");
        }

        private async Task<ReturnRequest> GetOrThrowAsync(int id)
            => await _repository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException("Không tìm thấy yêu cầu đổi/trả.");
    }
}
