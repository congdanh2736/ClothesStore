using ClothesStore.Api.DTOs.Customer;
using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService customerService)
        {
            _service = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CustomerDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            return customer is null ? NotFound() : Ok(customer);
        }

        //[HttpPost]
        //public async Task<ActionResult<CustomerDto>> Create(CreateCustomerDto dto)
        //{
        //    var (success, error, data) = await _service.CreateAsync(dto);
        //    if (!success) return BadRequest(new { message = error });

        //    return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        //}

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateCustomerDto dto)
        {
            var (success, error) = await _service.UpdateAsync(id, dto);
            if (!success)
                return error == "Không tìm thấy khách hàng." ? NotFound(new { message = error }) : BadRequest(new { message = error });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
