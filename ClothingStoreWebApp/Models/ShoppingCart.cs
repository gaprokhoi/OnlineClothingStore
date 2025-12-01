using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class ShoppingCart
    {
        [Key]
        public int CartID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int VariantID { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ProductVariant ProductVariant { get; set; }
    }
}