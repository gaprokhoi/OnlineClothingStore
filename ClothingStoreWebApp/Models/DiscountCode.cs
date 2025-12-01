using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class DiscountCode
    {
        [Key]
        public int DiscountID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } // 'Percentage', 'FixedAmount'

        [Required]
        
        public decimal DiscountValue { get; set; }

        
        public decimal? MinimumOrderAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<OrderDiscount> OrderDiscounts { get; set; }
       


        // Helper method to check if discount is valid
        public bool IsValidForDate(DateTime date)
        {
            return IsActive && date >= StartDate && date <= EndDate;
        }

        // Helper method to calculate discount amount
        public decimal CalculateDiscount(decimal orderAmount)
        {
            if (!IsActive) return 0;

            if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value)
                return 0;

            if (DiscountType == "Percentage")
            {
                return orderAmount * (DiscountValue / 100);
            }
            else if (DiscountType == "FixedAmount")
            {
                return Math.Min(DiscountValue, orderAmount); // Don't exceed order amount
            }

            return 0;
        }
    }
}