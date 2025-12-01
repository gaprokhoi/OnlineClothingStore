// Models/Inventory.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required]
        [Index("IX_Inventory_VariantID", IsUnique = true)]
        public int VariantID { get; set; }

        public int QuantityOnHand { get; set; } = 0;
        public int QuantityReserved { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("VariantID")]
        public virtual ProductVariant ProductVariant { get; set; }

        // Computed Property
        [NotMapped]
        public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    }
}