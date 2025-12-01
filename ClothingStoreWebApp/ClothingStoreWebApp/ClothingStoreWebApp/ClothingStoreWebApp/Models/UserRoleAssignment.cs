using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStoreWebApp.Models
{
    public class UserRoleAssignment
    {
        [Key, Column(Order = 0)]
        public int UserID { get; set; }

        [Key, Column(Order = 1)]
        public int RoleID { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual UserRole UserRole { get; set; }
    }
}