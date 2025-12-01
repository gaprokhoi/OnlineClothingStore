using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class User
    {
        [Key]

        public int UserID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }
        

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation Properties
        public virtual ICollection<UserAddress> UserAddresses { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<ShoppingCart> ShoppingCarts { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
        public virtual ICollection<ProductReview> ProductReviews { get; set; }
        public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; }

    }
}