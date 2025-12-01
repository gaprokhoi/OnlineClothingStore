using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Helpers;

namespace ClothingStoreWebApp.Controllers
{
    public class AdminController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        // Middleware để check admin cho tất cả actions
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Session.IsAdmin())
            {
                filterContext.Result = new RedirectResult("~/User/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: /Admin (Dashboard với product table)
        public ActionResult Index(int? categoryId, int? brandId, string searchTerm, int page = 1)
        {
            var products = db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Collection)
                .Include(p => p.ProductImages);

            // Apply filters
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                products = products.Where(p => p.BrandID == brandId.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.ProductName.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            // Pagination
            int pageSize = 15; // Admin view more items
            int totalProducts = products.Count();
            var pagedProducts = products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Statistics for dashboard
            ViewBag.TotalProducts = db.Products.Count(p => p.IsActive);
            ViewBag.TotalCategories = db.Categories.Count(c => c.IsActive);
            ViewBag.TotalBrands = db.Brands.Count(b => b.IsActive);
            ViewBag.InactiveProducts = db.Products.Count(p => !p.IsActive);

            // Pass data to view
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            ViewBag.BrandID = new SelectList(db.Brands, "BrandID", "BrandName");
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentBrandId = brandId;
            ViewBag.CurrentSearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.TotalProductsFound = totalProducts;

            return View(pagedProducts);
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
