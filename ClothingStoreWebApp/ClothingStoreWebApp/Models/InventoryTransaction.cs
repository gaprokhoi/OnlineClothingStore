// Models/InventoryTransaction.cs - MỚI HOÀN TOÀN
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public int VariantID { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; }  // "IN", "OUT", "ADJUST", "RESERVE", "RELEASE"

        [Required]
        public int Quantity { get; set; }  // + or -

        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        public int? OrderID { get; set; }
        public int? UserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("VariantID")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
    }
}
