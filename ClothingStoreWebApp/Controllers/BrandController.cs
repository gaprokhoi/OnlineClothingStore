using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;

namespace ClothingStoreWebApp.Controllers
{
    public class BrandController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        #region HELPER METHOD - Admin Check

        private bool IsAdmin()
        {
            if (Session["Role"] != null && Session["Role"].ToString() == "Admin")
                return true;

            var userRoles = Session["UserRoles"] as System.Collections.Generic.List<string>;
            return userRoles != null && userRoles.Contains("Admin");
        }

        private ActionResult RedirectIfNotAdmin()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        #endregion

        #region Admin Brand Management

        // GET: Brand/Index (Admin only)
        public ActionResult Index()
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            var brands = db.Brands
                .Include(b => b.Products)
                .OrderBy(b => b.BrandName)
                .ToList();

            return View(brands);
        }

        // GET: Brand/Details/5
        public ActionResult Details(int? id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Brand brand = db.Brands
                .Include(b => b.Products)
                .FirstOrDefault(b => b.BrandID == id);

            if (brand == null)
            {
                return HttpNotFound();
            }

            return View(brand);
        }

        // GET: Brand/Create
        public ActionResult Create()
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            return View();
        }

        // POST: Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "BrandName,Description,LogoURL,Website,IsActive")] Brand brand)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            if (ModelState.IsValid)
            {
                try
                {
                    brand.CreatedAt = DateTime.Now;
                    db.Brands.Add(brand);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã thêm thương hiệu '{brand.BrandName}' thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }
            return View(brand);
        }

        // GET: Brand/Edit/5
        public ActionResult Edit(int? id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Brand brand = db.Brands.Find(id);
            if (brand == null)
            {
                return HttpNotFound();
            }

            return View(brand);
        }

        // POST: Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BrandID,BrandName,Description,LogoURL,Website,IsActive,CreatedAt")] Brand brand)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(brand).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã cập nhật thương hiệu '{brand.BrandName}' thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }
            return View(brand);
        }

        // POST: Brand/SoftDelete/5 - Ẩn thương hiệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            try
            {
                Brand brand = db.Brands.Find(id);
                if (brand != null)
                {
                    brand.IsActive = false;
                    db.Entry(brand).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã ẩn thương hiệu '{brand.BrandName}' thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Brand/Restore/5 - Khôi phục thương hiệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(int id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            try
            {
                Brand brand = db.Brands.Find(id);
                if (brand != null)
                {
                    brand.IsActive = true;
                    db.Entry(brand).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã khôi phục thương hiệu '{brand.BrandName}' thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Brand/Delete/5 - Xác nhận xóa vĩnh viễn
        public ActionResult Delete(int? id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Brand brand = db.Brands
                .Include(b => b.Products)
                .FirstOrDefault(b => b.BrandID == id);

            if (brand == null)
            {
                return HttpNotFound();
            }

            return View(brand);
        }

        // POST: Brand/Delete/5 - Xóa vĩnh viễn
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var redirectResult = RedirectIfNotAdmin();
            if (redirectResult != null) return redirectResult;

            try
            {
                Brand brand = db.Brands
                    .Include(b => b.Products)
                    .FirstOrDefault(b => b.BrandID == id);

                if (brand != null)
                {
                    // Kiểm tra có sản phẩm không
                    if (brand.Products != null && brand.Products.Any())
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa! Thương hiệu này có {brand.Products.Count} sản phẩm. Vui lòng xóa hoặc chuyển sản phẩm sang thương hiệu khác trước.";
                        return RedirectToAction("Index");
                    }

                    // Xóa vĩnh viễn
                    db.Brands.Remove(brand);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn thương hiệu '{brand.BrandName}' thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa thương hiệu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Public Brand Pages

        // GET: Brand/List (Public)
        public ActionResult List()
        {
            var brands = db.Brands
                .Include(b => b.Products)
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToList();

            var categories = db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToList();

            ViewBag.Categories = categories;
            return View(brands);
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
