using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ClothingStoreWebApp.Controllers
{
    public class WishlistController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        // Helper: Get Current UserID
        private int GetCurrentUserId()
        {
            if (Session["UserID"] != null)
            {
                return (int)Session["UserID"];
            }
            return 0;
        }

        // GET: Wishlist/Index
        public ActionResult Index()
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách yêu thích!";
                return RedirectToAction("Login", "User");
            }

            int userId = GetCurrentUserId();

            var wishlistItems = db.Wishlists
                .Include(w => w.Product)
                .Include(w => w.Product.ProductImages)
                .Include(w => w.Product.Brand)
                .Include(w => w.Product.Category)
                .Where(w => w.UserID == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToList();

            return View(wishlistItems);
        }

        // POST: Wishlist/Add
        [HttpPost]
        public JsonResult Add(int productId)
        {
            try
            {
                if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                }

                int userId = GetCurrentUserId();

                // Check if already in wishlist
                var existing = db.Wishlists.FirstOrDefault(w => w.UserID == userId && w.ProductID == productId);

                if (existing != null)
                {
                    return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích" });
                }

                // Check if product exists
                var product = db.Products.Find(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Add to wishlist
                var wishlistItem = new Wishlist
                {
                    UserID = userId,
                    ProductID = productId,
                    AddedAt = DateTime.Now
                };

                db.Wishlists.Add(wishlistItem);
                db.SaveChanges();

                // Get wishlist count
                int wishlistCount = db.Wishlists.Count(w => w.UserID == userId);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm vào danh sách yêu thích!",
                    wishlistCount = wishlistCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Wishlist Add Error: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // POST: Wishlist/Remove
        [HttpPost]
        public JsonResult Remove(int productId)
        {
            try
            {
                if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                int userId = GetCurrentUserId();

                var wishlistItem = db.Wishlists.FirstOrDefault(w => w.UserID == userId && w.ProductID == productId);

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong danh sách" });
                }

                db.Wishlists.Remove(wishlistItem);
                db.SaveChanges();

                // Get wishlist count
                int wishlistCount = db.Wishlists.Count(w => w.UserID == userId);

                return Json(new
                {
                    success = true,
                    message = "Đã xóa khỏi danh sách yêu thích!",
                    wishlistCount = wishlistCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Wishlist Remove Error: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // POST: Wishlist/Toggle (Thêm/Xóa thông minh)
        [HttpPost]
        public JsonResult Toggle(int productId)
        {
            try
            {
                if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
                }

                int userId = GetCurrentUserId();

                var existing = db.Wishlists.FirstOrDefault(w => w.UserID == userId && w.ProductID == productId);

                if (existing != null)
                {
                    // Remove
                    db.Wishlists.Remove(existing);
                    db.SaveChanges();

                    int count = db.Wishlists.Count(w => w.UserID == userId);

                    return Json(new
                    {
                        success = true,
                        inWishlist = false,
                        message = "Đã xóa khỏi danh sách yêu thích",
                        wishlistCount = count
                    });
                }
                else
                {
                    // Add
                    var product = db.Products.Find(productId);
                    if (product == null)
                    {
                        return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                    }

                    db.Wishlists.Add(new Wishlist
                    {
                        UserID = userId,
                        ProductID = productId,
                        AddedAt = DateTime.Now
                    });

                    db.SaveChanges();

                    int count = db.Wishlists.Count(w => w.UserID == userId);

                    return Json(new
                    {
                        success = true,
                        inWishlist = true,
                        message = "Đã thêm vào danh sách yêu thích!",
                        wishlistCount = count
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Wishlist Toggle Error: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // GET: Wishlist/GetCount
        public JsonResult GetCount()
        {
            try
            {
                if (Session["IsLoggedIn"] != null && (bool)Session["IsLoggedIn"])
                {
                    int userId = GetCurrentUserId();
                    int count = db.Wishlists.Count(w => w.UserID == userId);
                    return Json(new { count = count }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
