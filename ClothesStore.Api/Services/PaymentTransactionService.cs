using AutoMapper;
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
        var transaction = await _repository.GetByIdAsync(id);
        return transaction is null ? null : _mapper.Map<PaymentTransactionDto>(transaction);
    }

    public async Task<(bool Success, string? Error, PaymentTransactionDto? Data)> CreateAsync(CreatePaymentTransactionRequest request)
    {
        var paymentMethod = request.PaymentMethod.Trim();
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return (false, "Phương thức thanh toán là bắt buộc.", null);

        if (!await _context.Orders.AnyAsync(order => order.Id == request.OrderId))
            return (false, "Không tìm thấy đơn hàng.", null);

        if (await _context.PaymentTransactions.AnyAsync(transaction => transaction.OrderId == request.OrderId))
            return (false, "Đơn hàng đã có giao dịch thanh toán.", null);

        var transaction = _mapper.Map<PaymentTransaction>(request);
        transaction.PaymentMethod = paymentMethod;
        transaction.Status = "Pending";
        await _repository.AddAsync(transaction);
        return (true, null, _mapper.Map<PaymentTransactionDto>(transaction));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdatePaymentTransactionRequest request)
    {
        var paymentMethod = request.PaymentMethod.Trim();
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return (false, "Phương thức thanh toán là bắt buộc.");

        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return (false, "Không tìm thấy giao dịch thanh toán.");

        _mapper.Map(request, transaction);
        transaction.PaymentMethod = paymentMethod;
        await _repository.UpdateAsync(transaction);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return false;

        await _repository.DeleteAsync(transaction);
        return true;
    }
}
