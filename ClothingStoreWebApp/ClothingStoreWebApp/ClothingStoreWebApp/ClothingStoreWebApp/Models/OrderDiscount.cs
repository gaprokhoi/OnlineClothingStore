using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class OrderDiscount
    {
        [Key]
        public int OrderID { get; set; }

        [Key]
        public int DiscountID { get; set; }

        [Required]
        
        public decimal DiscountAmount { get; set; }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual DiscountCode DiscountCode { get; set; }
    }
}