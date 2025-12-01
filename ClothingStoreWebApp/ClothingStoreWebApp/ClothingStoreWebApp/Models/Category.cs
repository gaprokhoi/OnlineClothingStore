using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class Category
    {
        [Key]

        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        public int? ParentCategoryID { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(255)]
        public string ImageURL { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}