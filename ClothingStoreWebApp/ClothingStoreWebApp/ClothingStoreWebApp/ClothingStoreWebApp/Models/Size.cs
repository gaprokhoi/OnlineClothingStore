using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class Size
    {
        [Key]

        public int SizeID { get; set; }

        [Required]
        [StringLength(20)]
        public string SizeName { get; set; }

        [Required]
        public int SizeOrder { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<ProductVariant> ProductVariants { get; set; }

    }
}