using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int ProductID { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Product Product { get; set; }
    }
}
