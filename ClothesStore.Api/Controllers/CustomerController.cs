using ClothesStore.Api.Interface.Services;
using ClothesStore.Api.Services;

namespace ClothesStore.Api.Controllers
{
    public class CustomerController
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }


    }
}
