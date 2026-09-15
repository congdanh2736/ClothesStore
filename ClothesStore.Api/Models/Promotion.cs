using System.ComponentModel.DataAnnotations;

namespace ClothesStore.Api.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; }
        public decimal DiscountValue { get; set; }
        public ICollection<Order> Orders = new List<Order>();
    }
}
