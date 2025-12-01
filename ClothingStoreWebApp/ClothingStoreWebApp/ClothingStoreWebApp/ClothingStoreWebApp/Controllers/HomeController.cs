
using ClothingStoreWebApp.Data;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ClothingStoreWebApp.Controllers
{
    public class HomeController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        public ActionResult Index()
        {
            // Get featured products for homepage
            var featuredProducts = db.Products
                .Where(p => p.IsFeatured && p.IsActive)
                .Take(8)
                .ToList();

            return View(featuredProducts);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
        public ActionResult TestDB()
        {
            try
            {
                using (var db = new ClothingStoreDbContext())
                {
                    // Test simple query
                    var productCount = db.Products.Count();
                    ViewBag.Message = $"Kết nối thành công! Database có {productCount} sản phẩm.";
                    return Content(ViewBag.Message);
                }
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
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