using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; }

        [Required]
        public int UserID { get; set; }

        public int? OrderStatusID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Pricing
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal ShippingAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        [StringLength(500)]
        public string CancellationReason { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }


        // Shipping Address
        [Required]
        [StringLength(200)]
        public string ShippingFullName { get; set; }

        [Required]
        [StringLength(255)]
        public string ShippingAddressLine1 { get; set; }

        [StringLength(255)]
        public string ShippingAddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingCity { get; set; }

        [Required]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingCountry { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(100)]
        public string TrackingNumber { get; set; }

        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public bool IsGift { get; set; } = false;

        [StringLength(500)]
        public string GiftMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("OrderStatusID")]
        public virtual OrderStatus OrderStatus { get; set; }

        // FIX: Add generic types
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; }
    }
}
