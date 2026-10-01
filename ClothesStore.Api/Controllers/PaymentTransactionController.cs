using ClothesStore.Api.DTOs.PaymentTransaction;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentTransactionController : ControllerBase
{
    private readonly IPaymentTransactionService _service;

    public PaymentTransactionController(IPaymentTransactionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentTransactionDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentTransactionDto>> GetById(int id)
    {
        var transaction = await _service.GetByIdAsync(id);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentTransactionDto>> Create(CreatePaymentTransactionRequest request)
    {
        var (success, error, data) = await _service.CreateAsync(request);
        if (!success)
            return error == "Không tìm thấy đơn hàng."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePaymentTransactionRequest request)
    {
        var (success, error) = await _service.UpdateAsync(id, request);
        if (!success)
            return error == "Không tìm thấy giao dịch thanh toán."
                ? NotFound(new { message = error })
                : BadRequest(new { message = error });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
