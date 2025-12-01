using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class UserAddress
    {
        [Key]
        public int AddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(20)]
        public string AddressType { get; set; } // 'Shipping', 'Billing'

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required]
        [StringLength(255)]
        public string AddressLine1 { get; set; }

        [StringLength(255)]
        public string AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual User User { get; set; }
    }
}