using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(255)]
        public string ReviewTitle { get; set; }

        public string ReviewText { get; set; }

        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual User User { get; set; }
    }
}