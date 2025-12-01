using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class SeasonalCollection
    {
        [Key]
        public int CollectionID { get; set; }

        [Required]
        [StringLength(100)]
        public string CollectionName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(255)]
        public string ImageURL { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; }
    }
}
