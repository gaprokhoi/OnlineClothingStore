using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public int VariantID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        // Snapshot data at time of order
        [Required]
        [StringLength(255)]
        public string ProductName { get; set; }

        [Required]
        [StringLength(100)]
        public string SKU { get; set; }

        [Required]
        [StringLength(50)]
        public string ColorName { get; set; }

        [Required]
        [StringLength(20)]
        public string SizeName { get; set; }

        // Navigation Properties
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }

        [ForeignKey("VariantID")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}
