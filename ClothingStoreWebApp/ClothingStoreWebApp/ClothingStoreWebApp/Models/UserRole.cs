using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.Models
{
    public class UserRole
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        // Navigation Properties
        public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; }
    }
}