using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public ICollection<Order> Orders = new List<Order>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
