using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;

namespace ClothingStoreWebApp.Controllers
{
    public class CategoryController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        // GET: Category/List - Public category listing
        public ActionResult List()
        {
            var categories = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            var brands = db.Brands
                .Include(b => b.Products)
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToList();

            ViewBag.Brands = brands;

            return View(categories);
        }



        #region ADMIN MANAGEMENT ACTIONS

        // GET: Category/Index - Admin Management Page
        public ActionResult Index()
        {
            // Debug
            var role = Session["Role"]?.ToString();
            var isLoggedIn = Session["IsLoggedIn"];

            if (string.IsNullOrEmpty(role) || role != "Admin")
            {
                TempData["ErrorMessage"] = $"Không có quyền. Role: {role}, IsLoggedIn: {isLoggedIn}";
                return RedirectToAction("Index", "Home");
            }

            var categories = db.Categories
              .Include(c => c.ParentCategory)
              .Include(c => c.SubCategories)
              .Include(c => c.Products)
              .OrderBy(c => c.ParentCategoryID.HasValue ? 1 : 0)  // Danh mục gốc trước
              .ThenBy(c => c.ParentCategoryID)                     // Nhóm theo parent
              .ThenBy(c => c.SortOrder)                            // Sắp xếp theo SortOrder
              .ThenBy(c => c.CategoryName)                         // Rồi theo tên
              .ToList();

            return View(categories);
        }


        // GET: Category/Details/5 - Admin View Details
        public ActionResult Details(int? id)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // GET: Category/Create
        public ActionResult Create()
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            // ✅ Load TẤT CẢ categories (cả gốc và con) cho dropdown
            // Để trống = tạo danh mục gốc
            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName"
            );

            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                db.Categories.Add(category);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Reload dropdown on validation error
            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // GET: Category/Edit/5
        public ActionResult Edit(int? id)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            // Load parent categories for dropdown, excluding self and its children
            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.CategoryID != id && c.ParentCategoryID == null).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category category)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (ModelState.IsValid)
            {
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Reload dropdown on validation error
            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.CategoryID != category.CategoryID && c.ParentCategoryID == null).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // GET: Category/Delete/5
        public ActionResult Delete(int? id)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            try
            {
                Category category = db.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefault(c => c.CategoryID == id);

                if (category != null)
                {
                    // Kiểm tra có danh mục con không (kể cả đã ẩn)
                    if (category.SubCategories != null && category.SubCategories.Any())
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa! Danh mục này có {category.SubCategories.Count} danh mục con. Vui lòng xóa các danh mục con trước.";
                        return RedirectToAction("Index");
                    }

                    // Kiểm tra có sản phẩm không (kể cả đã ẩn)
                    if (category.Products != null && category.Products.Any())
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa! Danh mục này có {category.Products.Count} sản phẩm. Vui lòng xóa hoặc chuyển sản phẩm sang danh mục khác trước.";
                        return RedirectToAction("Index");
                    }

                    // Xóa vĩnh viễn
                    db.Categories.Remove(category);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn danh mục '{category.CategoryName}' thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa danh mục: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Category/SoftDelete/5 - Hide category instead of delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                Category category = db.Categories.Find(id);

                if (category != null)
                {
                    category.IsActive = false;
                    db.Entry(category).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã ẩn danh mục '{category.CategoryName}' thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi ẩn danh mục: " + ex.Message;
            }

            return RedirectToAction("Index");
        }


        // POST: Category/Restore/5 - Restore hidden category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(int id)
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                Category category = db.Categories.Find(id);

                if (category != null)
                {
                    category.IsActive = true;
                    db.Entry(category).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã khôi phục danh mục '{category.CategoryName}' thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi khôi phục danh mục: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        #endregion



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
