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

        #region Public Views (Read-Only for Users & Admins)

        // GET: Category/List - Public view for all users
        public ActionResult List()
        {
            // Get all active categories
            var categories = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            // Get all active brands
            var brands = db.Brands
                .Include(b => b.Products)
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToList();

            // Pass brands to ViewBag
            ViewBag.Brands = brands;

            return View(categories);
        }


        // GET: Category/ViewDetails/5 - Public detail view
        public ActionResult ViewDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Category category = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.Products.Select(p => p.Brand))
                .Include(c => c.Products.Select(p => p.ProductImages))
                .FirstOrDefault(c => c.CategoryID == id && c.IsActive);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Get all categories and brands for sidebar
            ViewBag.AllCategories = db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToList();

            ViewBag.Brands = db.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToList();

            return View(category);
        }

        #endregion

        #region Admin Management (Create/Edit/Delete - Admin Only)



        #endregion

        #region Admin Management (Create/Edit/Delete - Admin Only)

        // GET: Category/Manage - Admin management page
        [HttpGet]
        public ActionResult Manage()
        {
            // Check if admin
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            var categories = db.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .ToList();

            return View(categories);
        }

        // GET: Category/Create - Admin only
        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.ParentCategoryID == null)
                    .OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName"
            );

            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CategoryName,ParentCategoryID,Description,ImageURL,SortOrder,IsActive")] Category category)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    category.CreatedAt = DateTime.Now;
                    db.Categories.Add(category);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã thêm danh mục '{category.CategoryName}' thành công!";
                    return RedirectToAction("Manage");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.ParentCategoryID == null)
                    .OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // GET: Category/Edit/5 - Admin only
        public ActionResult Edit(int? id)
        {
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

            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.ParentCategoryID == null && c.CategoryID != id)
                    .OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CategoryID,CategoryName,ParentCategoryID,Description,ImageURL,SortOrder,IsActive,CreatedAt")] Category category)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(category).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{category.CategoryName}' thành công!";
                    return RedirectToAction("Manage");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            ViewBag.ParentCategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive && c.ParentCategoryID == null && c.CategoryID != category.CategoryID)
                    .OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                category.ParentCategoryID
            );

            return View(category);
        }

        // POST: Category/SoftDelete/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
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
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Manage");
        }

        // POST: Category/Restore/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(int id)
        {
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
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Manage");
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
