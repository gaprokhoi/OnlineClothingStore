// ViewModels/ResetPasswordViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace ClothingStoreWebApp.ViewModels
{
    /// Sử dụng khi người dùng click vào liên kết đặt lại mật khẩu từ email

    public class ResetPasswordViewModel
    {
        
        /// Token được sinh và lưu khi gửi email quên mật khẩu
        /// </summary>
        [Required]
        public string Token { get; set; }

        
        /// Email của user để xác thực token khớp với đúng user
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        
        /// Mật khẩu mới
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
        public string NewPassword { get; set; }

        
        /// Xác nhận mật khẩu mới
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}
