using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserID { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự")]
        [Display(Name = "Tên")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Họ không được để trống")]
        [StringLength(50, ErrorMessage = "Họ không được quá 50 ký tự")]
        [Display(Name = "Họ")]
        public string LastName { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^[0-9\s\-\+\(\)]*$", ErrorMessage = "Số điện thoại chỉ chứa số và ký tự đặc biệt")]
        public string PhoneNumber { get; set; }

        // Computed property
        public string FullName => $"{FirstName} {LastName}";
    }
}
