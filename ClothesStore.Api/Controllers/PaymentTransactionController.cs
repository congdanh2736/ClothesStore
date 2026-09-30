using AutoMapper;
using ClothesStore.Api.Data;
using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentTransactionController : ControllerBase
{
    private readonly IPaymentMethodRepository _repository;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PaymentTransactionController(IPaymentMethodRepository repository, ApplicationDbContext context, IMapper mapper)
    {
        _repository = repository;
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentTransactionDto>>> GetAll()
        => Ok(_mapper.Map<IEnumerable<PaymentTransactionDto>>(await _repository.GetAllAsync()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentTransactionDto>> GetById(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        return transaction is null ? NotFound() : Ok(_mapper.Map<PaymentTransactionDto>(transaction));
    }

    [HttpPost("orders/{orderId:int}")]
    public async Task<ActionResult<PaymentTransactionDto>> Create(int orderId, CreatePaymentTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return BadRequest(new { message = "Phương thức thanh toán là bắt buộc." });

        if (!await _context.Orders.AnyAsync(order => order.Id == orderId))
            return NotFound(new { message = "Không tìm thấy đơn hàng." });

        if (await _context.PaymentTransactions.AnyAsync(transaction => transaction.OrderId == orderId))
            return Conflict(new { message = "Đơn hàng đã có giao dịch thanh toán." });

        var transaction = _mapper.Map<PaymentTransaction>(request);
        transaction.OrderId = orderId;
        transaction.Status = "Pending";
        await _repository.AddAsync(transaction);

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, _mapper.Map<PaymentTransactionDto>(transaction));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePaymentTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return BadRequest(new { message = "Phương thức thanh toán là bắt buộc." });

        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return NotFound();

        _mapper.Map(request, transaction);
        await _repository.UpdateAsync(transaction);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction is null)
            return NotFound();

        await _repository.DeleteAsync(transaction);
        return NoContent();
    }
}
