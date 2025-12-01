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
            //Check Admin role
            if (roles.Contains("Admin"))
            {
                Session["Role"] = "Admin";
            }
            else
            {
                Session["Role"] = "Customer";
            }

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


        // GET: User/Profile
        public new ActionResult Profile()
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }

            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var user = db.Users
                .Include(u => u.UserAddresses)
                .FirstOrDefault(u => u.UserID == userId);

            if (user == null)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }

            // Get order statistics
            var orderStats = new
            {
                TotalOrders = db.Orders.Count(o => o.UserID == userId),
                PendingOrders = db.Orders.Count(o => o.UserID == userId && o.OrderStatus.StatusName == "Pending"),
                CompletedOrders = db.Orders.Count(o => o.UserID == userId && o.OrderStatus.StatusName == "Delivered"),
                TotalSpent = db.Orders
                    .Where(o => o.UserID == userId && o.OrderStatus.StatusName == "Delivered")
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0
            };

            ViewBag.OrderStats = orderStats;

            var vm = new UserProfileViewModel
            {
                UserID = user.UserID,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
            };

            return View(vm);
        }


        // POST: User/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UserProfileViewModel model)
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View("Profile", model);
            }

            try
            {
                int userId = GetCurrentUserId();
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    Session.Clear();
                    return RedirectToAction("Login");
                }

                // Validate Phone Number Format (optional)
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    // Remove spaces and check if it's digits only
                    string cleanPhone = model.PhoneNumber.Replace(" ", "").Replace("-", "");
                    if (!cleanPhone.All(char.IsDigit) || cleanPhone.Length < 9 || cleanPhone.Length > 11)
                    {
                        ModelState.AddModelError("PhoneNumber", "Số điện thoại không hợp lệ (9-11 chữ số).");
                        TempData["ErrorMessage"] = "Số điện thoại không hợp lệ!";
                        return View("Profile", model);
                    }
                }

                // Update user info
                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();
                user.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                // Update session
                Session["UserName"] = $"{user.FirstName} {user.LastName}";

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProfile error: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin.";
                return View("Profile", model);
            }
        }


        // CHANGE PASSWORD
        //get: User/ChangePassword
        public ActionResult ChangePassword()
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }
            return View(new ChangePasswordViewModel());
        }
        // POST: User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            // Check login
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Login");
            }

            // ✅ CHECK MODEL STATE FIRST
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View(model);
            }

            int uid = GetCurrentUserId();
            if (uid == 0)
            {
                return RedirectToAction("Login");
            }

            var user = db.Users.Find(uid);
            if (user == null)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }

            // ✅ VERIFY CURRENT PASSWORD
            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                return View(model);
            }

            // ✅ CHECK IF NEW PASSWORD IS SAME AS CURRENT
            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới phải khác mật khẩu hiện tại.");
                TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu hiện tại!";
                return View(model);
            }

            // ✅ PASSWORD STRENGTH VALIDATION
            if (model.NewPassword.Length < 6)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu phải có ít nhất 6 ký tự.");
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View(model);
            }

            try
            {
                // ✅ UPDATE PASSWORD
                user.PasswordHash = HashSha256(model.NewPassword);
                user.UpdatedAt = DateTime.Now;

                // Clear reset token if exists
                user.ResetToken = null;
                user.ResetTokenExpiry = null;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";

                // Log security event
                System.Diagnostics.Debug.WriteLine($"[SECURITY] Password changed for UserID: {uid} at {DateTime.Now}");

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                }

                ModelState.AddModelError("", "Có lỗi xảy ra khi đổi mật khẩu.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu!";
                return View(model);
            }
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
