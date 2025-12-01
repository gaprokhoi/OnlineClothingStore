using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class Brand
    {
        [Key]

        public int BrandID { get; set; }

        [Required]
        [StringLength(100)]
        public string BrandName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(255)]
        public string LogoURL { get; set; }

        [StringLength(255)]
        public string Website { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; }
    }
}