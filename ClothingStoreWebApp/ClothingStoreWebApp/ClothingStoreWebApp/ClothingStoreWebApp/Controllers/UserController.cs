using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Helpers;
using ClothingStoreWebApp.Models;
using ClothingStoreWebApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace ClothingStoreWebApp.Controllers
{
    public class UserController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        // LOGIN
        // GET: /User/Login
        public ActionResult Login(string returnUrl)
        {
            // Nếu đã đăng nhập rồi, chuyển thẳng
            if (Session["IsLoggedIn"] != null && (bool)Session["IsLoggedIn"])
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            // Hiển thị form, giữ returnUrl trong ViewBag để view chèn hidden field
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /User/Login

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                
                return View(model);
            }

            var user = db.Users.FirstOrDefault(u => u.Email == model.Email && u.IsActive);
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // Load roles
            var roles = (from ura in db.UserRoleAssignments
                         join ur in db.UserRoles on ura.RoleID equals ur.RoleID
                         where ura.UserID == user.UserID
                         select ur.RoleName).ToList();

            // Gán mặc định Customer nếu chưa có vai trò
            if (!roles.Any())
            {
                var custRole = db.UserRoles.Single(r => r.RoleName == "Customer");
                db.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    UserID = user.UserID,
                    RoleID = custRole.RoleID,
                    AssignedAt = DateTime.Now
                });
                db.SaveChanges();
                roles = new List<string> { "Customer" };
            }

            // Lưu session
            Session["IsLoggedIn"] = true;
            Session["UserID"] = user.UserID;
            Session["UserName"] = $"{user.FirstName} {user.LastName}";
            Session["UserEmail"] = user.Email;
            Session["UserRoles"] = roles;

            System.Diagnostics.Debug.WriteLine($"Login success. Set session - UserID: {user.UserID}, UserName: {Session["UserName"]}");

            // Redirect logic đơn giản
            if (!string.IsNullOrEmpty(model.ReturnUrl)
                && Url.IsLocalUrl(model.ReturnUrl)
                && !model.ReturnUrl.ToLower().Contains("login"))
            {
                return Redirect(model.ReturnUrl);
            }

            // Mặc định về Profile thay vì Home
            return RedirectToAction("Profile");
        }

        // REGISTER
        public ActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (db.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }
            var user = new User
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = HashSha256(model.Password),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            db.Users.Add(user);
            db.SaveChanges();

            var custRole = db.UserRoles.Single(r => r.RoleName == "Customer");
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserID = user.UserID,
                RoleID = custRole.RoleID,
                AssignedAt = DateTime.Now
            });
            db.SaveChanges();
            Session.SetRoles(new List<string> { "Customer" });
            Session["UserID"] = user.UserID;
            Session["UserName"] = $"{user.FirstName} {user.LastName}";
            Session["UserEmail"] = user.Email;
            Session["IsLoggedIn"] = true;

            return RedirectToAction("Index", "Home");
        }


        // PROFILE
        // GET: User/Profile


        public ActionResult Profile()
        {
            // Debug session - không redirect gì cả
            System.Diagnostics.Debug.WriteLine($"Profile accessed. Session values:");
            System.Diagnostics.Debug.WriteLine($"IsLoggedIn: {Session["IsLoggedIn"]}");
            System.Diagnostics.Debug.WriteLine($"UserID: {Session["UserID"]}");
            System.Diagnostics.Debug.WriteLine($"UserName: {Session["UserName"]}");
            ViewBag.DebugInfo = $"IsLoggedIn: {Session["IsLoggedIn"]}, UserID: {Session["UserID"]}, UserName: {Session["UserName"]}";


            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }

            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var user = db.Users.Find(userId);
            if (user == null)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }

            //view model user 
            var vm = new UserProfileViewModel
            {
                UserID = user.UserID,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            return View(vm);
        }




        // POST: User/Profile

        [HttpPost, ValidateAntiForgeryToken]

        public ActionResult Profile(UserProfileViewModel model)
        {
            // Kiểm tra đăng nhập đầu tiên
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid) return View(model);
            int uid = GetCurrentUserId();
            if (uid == 0)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }

            var user = db.Users.Find(uid);
            if (user == null)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }
            // Kiểm tra email trùng lặp (chỉ khi user thay đổi email)
            if (user.Email != model.Email && db.Users.Any(u => u.Email == model.Email && u.UserID != uid))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }
            // Cập nhật thông tin user
            try
            {
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                // Cập nhật session UserName

                Session["UserName"] = $"{user.FirstName} {user.LastName}";
                Session["UserEmail"] = user.Email;
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                // Log exception nếu cần
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin.");
                return View(model);
            }
        }

        // CHANGE PASSWORD
     
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            int uid = GetCurrentUserId();
            var user = db.Users.Find(uid);
            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }
            user.PasswordHash = HashSha256(model.NewPassword);
            user.UpdatedAt = DateTime.Now;
            db.SaveChanges();
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // FORGOT PASSWORD
        public ActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = db.Users.SingleOrDefault(u => u.Email == model.Email && u.IsActive);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại.");
                return View(model);
            }

            user.ResetToken = Guid.NewGuid().ToString("N");
            user.ResetTokenExpiry = DateTime.Now.AddHours(2);
            db.SaveChanges();

            // TODO: Gửi email với link:
            // /User/ResetPassword?token={user.ResetToken}&email={user.Email}

            return View("ForgotPasswordConfirmation");
        }

        // GET: /User/ResetPassword
        public ActionResult ResetPassword(string token, string email)
        {
            var user = db.Users.SingleOrDefault(u
                => u.Email == email
                && u.ResetToken == token
                && u.ResetTokenExpiry > DateTime.Now);
            if (user == null) return View("InvalidToken");

            var vm = new ResetPasswordViewModel { Token = token, Email = email };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = db.Users.SingleOrDefault(u
                => u.Email == model.Email
                && u.ResetToken == model.Token
                && u.ResetTokenExpiry > DateTime.Now);
            if (user == null)
            {
                ModelState.AddModelError("", "Liên kết không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            user.PasswordHash = HashSha256(model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            return View("ResetPasswordConfirmation");
        }
        // LOGOUT
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        #region Helpers
        private string HashSha256(string input)
        {
            // Sử dụng using block thay vì using var
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private bool VerifyPassword(string pw, string hash) => HashSha256(pw) == hash;

        private int GetCurrentUserId() =>
            int.TryParse(Session["UserID"]?.ToString(), out var id) ? id : 0;
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
