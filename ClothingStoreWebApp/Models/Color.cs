using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class Color
    {
        [Key]

        public int ColorID { get; set; }

        [Required]
        [StringLength(50)]
        public string ColorName { get; set; }

        [Required]
        [StringLength(10)]
        public string ColorCode { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<ProductVariant> ProductVariants { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
    }
}


