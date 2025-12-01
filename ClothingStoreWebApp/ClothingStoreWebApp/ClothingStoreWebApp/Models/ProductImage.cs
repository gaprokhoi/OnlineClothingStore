using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }

        [Required]
        public int ProductID { get; set; }

        public int? ColorID { get; set; } // NULL means applies to all colors

        [Required]
        [StringLength(500)]
        public string ImageURL { get; set; }

        [StringLength(255)]
        public string AltText { get; set; }

        public int SortOrder { get; set; } = 0;

        [StringLength(50)]
        public string ImageType { get; set; } = "Product"; // 'Product', 'Thumbnail', 'Zoom', '360'

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual Color Color { get; set; }
    }
}