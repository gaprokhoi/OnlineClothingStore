// ViewModels/UserProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserID { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [Phone, StringLength(20)]
        public string PhoneNumber { get; set; }
    }

   
}
