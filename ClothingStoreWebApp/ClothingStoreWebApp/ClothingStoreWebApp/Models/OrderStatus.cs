using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class OrderStatus
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } // 'Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'

        [StringLength(255)]
        public string Description { get; set; }

        public int SortOrder { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; }
    }
}