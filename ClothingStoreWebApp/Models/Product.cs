using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class Product
    {
        [Key]

        public int ProductID { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; }

        public string Description { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        public int BrandID { get; set; }

        public int? CollectionID { get; set; }

        [StringLength(200)]
        public string Material { get; set; }

        [StringLength(500)]
        public string CareInstructions { get; set; }

        [Required]
        [StringLength(20)]
        public string Gender { get; set; }

        [Required]
        
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Category Category { get; set; }
        public virtual Brand Brand { get; set; }
        public virtual SeasonalCollection Collection { get; set; }
        public virtual ICollection<ProductVariant> ProductVariants { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
        public virtual ICollection<ProductReview> ProductReviews { get; set; }
    }
}
