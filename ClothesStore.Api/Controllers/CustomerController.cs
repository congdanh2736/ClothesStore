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
    }
}
