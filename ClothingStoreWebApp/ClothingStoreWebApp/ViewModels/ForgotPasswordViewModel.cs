// ViewModels/ForgotPasswordViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.ViewModels
{

    /// Dùng để nhập email khi người dùng quên mật khẩu.

    public class ForgotPasswordViewModel
    {

        /// Email đã đăng ký của người dùng.

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
