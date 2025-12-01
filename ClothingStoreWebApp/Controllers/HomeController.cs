using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;  // Namespace Data của bạn
using ClothingStoreWebApp.Models; // Namespace Models của bạn

namespace ClothingStoreWebApp.Controllers
{
    public class HomeController : Controller
    {
        // Khởi tạo DbContext của bạn
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        public ActionResult Index()
        {
            // --- 1. LẤY 4 SẢN PHẨM NỔI BẬT ---
            // Dùng .Include() để tải luôn ảnh sản phẩm, tránh lỗi N+1 query
            var featuredProducts = db.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && p.IsFeatured) // Lọc sản phẩm đang hoạt động & nổi bật
                .OrderByDescending(p => p.CreatedAt)   // Lấy sản phẩm nổi bật mới nhất
                .Take(4)                               // Giới hạn 4 sản phẩm
                .ToList();

            // --- 2. LẤY 16 SẢN PHẨM "GỢI Ý HÔM NAY" ---
            var todaySuggestions = db.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)   // Lấy sản phẩm mới nhất
                .Take(16)                              // Giới hạn 16 sản phẩm
                .ToList();

            // --- 3. GỬI 2 DANH SÁCH SANG VIEW ---
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.TodaySuggestions = todaySuggestions;

            return View();
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

        // Thêm hàm Dispose để giải phóng DbContext khi Controller không dùng nữa
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