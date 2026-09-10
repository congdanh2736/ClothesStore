using ClothesStore.src.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClothesStore.Pages
{
    public class CustomersModel : PageModel
    {
        private readonly MyDbContext _context;

        public CustomersModel(MyDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }
    }
}
