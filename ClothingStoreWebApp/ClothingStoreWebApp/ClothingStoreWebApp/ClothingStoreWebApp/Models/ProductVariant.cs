using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace ClothingStoreWebApp.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string SKU { get; set; }

        [Required]
        public int ColorID { get; set; }

        [Required]
        public int SizeID { get; set; }

        [Required]
        
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual Color Color { get; set; }
        public virtual Size Size { get; set; }
     
        public virtual ICollection<ShoppingCart> ShoppingCarts { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}