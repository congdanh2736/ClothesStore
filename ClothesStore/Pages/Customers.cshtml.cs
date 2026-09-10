using ClothesStore.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClothesStore.Pages
{
    public class CustomersModel : PageModel
    {
        private readonly MyDbContext _context;

        public List<Customer> Customers { get; set; } = new List<Customer>();

        [BindProperty]
        public Customer NewCustomer { get; set; }

        public CustomersModel(MyDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            Customers = _context.Customers.ToList();
        }

        public IActionResult OnPost()
        {
            _context.Customers.Add(NewCustomer);
            _context.SaveChanges();
            return RedirectToPage();
        }
    }
}
