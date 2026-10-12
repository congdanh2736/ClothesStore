using AutoMapper;
using ClothesStore.Api.Common; // [THÊM] PaymentStatus, PaymentMethods, OrderStatus, BusinessException
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Services;

public class PaymentTransactionService : IPaymentTransactionService
{
    private readonly IPaymentMethodRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PaymentTransactionService(IPaymentMethodRepository repository, ApplicationDbContext context, IMapper mapper)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PaymentTransactionDto>> GetAllAsync()
        => _mapper.Map<IEnumerable<PaymentTransactionDto>>(await _repository.GetAllAsync());

    public async Task<PaymentTransactionDto?> GetByIdAsync(int id)
    {
        // [SỬA] GetByIdAsync -> GetByIdWithDetailsAsync để DTO có OrderTotal
        var transaction = await _repository.GetByIdWithDetailsAsync(id);
        return transaction is null ? null : _mapper.Map<PaymentTransactionDto>(transaction);
    }

    // [THÊM comment] Đơn tạo qua API Order đã tự có giao dịch; hàm này dùng cho đơn cũ chưa có giao dịch
    public async Task<(bool Success, string? Error, PaymentTransactionDto? Data)> CreateAsync(CreatePaymentTransactionRequest request)
    {
        // [SỬA] Chỉ nhận phương thức thanh toán trong danh sách hỗ trợ (trước đây nhận chuỗi bất kỳ)
        var paymentMethod = NormalizeMethod(request.PaymentMethod);
        if (paymentMethod is null)
            return (false, "Phương thức thanh toán không hợp lệ.", null);

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order is null)
            return (false, "Không tìm thấy đơn hàng.", null);

        // [THÊM] Không tạo thanh toán cho đơn đã hủy
        if (order.Status == OrderStatus.Cancelled)
            return (false, "Đơn hàng đã bị hủy.", null);

        if (await _context.PaymentTransactions.AnyAsync(transaction => transaction.OrderId == request.OrderId))
            return (false, "Đơn hàng đã có giao dịch thanh toán.", null);

        // [SỬA] tạo trực tiếp thay vì AutoMapper (map CreatePaymentTransactionRequest đã không còn cần)
        var transaction = new PaymentTransaction
        {
            OrderId = request.OrderId,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending
        };
        await _repository.AddAsync(transaction);

        // [SỬA] load lại kèm Order để DTO có OrderTotal
        var created = await _repository.GetByIdWithDetailsAsync(transaction.Id);
        return (true, null, _mapper.Map<PaymentTransactionDto>(created));
    }

    // [SỬA] Đổi phương thức thanh toán: chỉ khi chưa thanh toán và đơn còn Pending (trước đây đổi lúc nào cũng được)
    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdatePaymentTransactionRequest request)
    {
        var paymentMethod = NormalizeMethod(request.PaymentMethod);
        if (paymentMethod is null)
            return (false, "Phương thức thanh toán không hợp lệ.");

        // [SỬA] GetByIdAsync -> GetByIdWithDetailsAsync để kiểm tra trạng thái đơn
        var transaction = await _repository.GetByIdWithDetailsAsync(id);
        if (transaction is null)
            return (false, "Không tìm thấy giao dịch thanh toán.");

        // [THÊM]
        if (transaction.Status == PaymentStatus.Paid)
            return (false, "Giao dịch đã thanh toán, không thể đổi phương thức.");

        // [THÊM]
        if (transaction.Order is not null && transaction.Order.Status != OrderStatus.Pending)
            return (false, "Chỉ được đổi phương thức thanh toán khi đơn hàng đang chờ xác nhận.");

        // [XÓA] _mapper.Map(request, transaction); (gán trực tiếp bên dưới là đủ)
        transaction.PaymentMethod = paymentMethod;
        transaction.Status = PaymentStatus.Pending; // [THÊM] đổi phương thức -> giao dịch quay về chờ thanh toán
        await _repository.UpdateAsync(transaction);
        return (true, null);
    }

    // [THÊM] Ghi nhận kết quả thanh toán từ cổng thanh toán / nhân viên (trước đây không có cách nào đổi Status)
    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, UpdatePaymentStatusRequest request)
    {
        var transaction = await _repository.GetByIdWithDetailsAsync(id);
        if (transaction is null)
            return (false, "Không tìm thấy giao dịch thanh toán.");

        if (transaction.Order is not null && transaction.Order.Status == OrderStatus.Cancelled)
            return (false, "Đơn hàng đã bị hủy, không thể cập nhật thanh toán.");

        if (transaction.Status != PaymentStatus.Pending && transaction.Status != PaymentStatus.Failed)
            return (false, $"Không thể chuyển giao dịch từ trạng thái {transaction.Status} sang {request.Status}.");

        transaction.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.TransactionCode))
            transaction.TransactionCode = request.TransactionCode.Trim();
        transaction.PaidAt = request.Status == PaymentStatus.Paid ? DateTime.UtcNow : null;

        await _repository.UpdateAsync(transaction);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return false;

        // [THÊM] Không xóa giao dịch đã thanh toán (mất dấu tiền)
        if (transaction.Status == PaymentStatus.Paid)
            throw new BusinessException("Không thể xóa giao dịch đã thanh toán.");

        await _repository.DeleteAsync(transaction);
        return true;
    }

    // [THÊM] Chuẩn hóa tên phương thức (không phân biệt hoa thường), trả null nếu không hỗ trợ
    private static string? NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return null;
        return PaymentMethods.All.FirstOrDefault(m => string.Equals(m, method.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
