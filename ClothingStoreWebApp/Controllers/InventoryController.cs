using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;
using ClothingStoreWebApp.ViewModels;
using ClothingStoreWebApp.Helpers;

namespace ClothingStoreWebApp.Controllers
{
    public class InventoryController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();
        private const int LOW_STOCK_THRESHOLD = 20;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Session.IsAdmin())
            {
                filterContext.Result = new RedirectResult("~/User/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: Inventory
        public ActionResult Index(int? categoryId, int? brandId, string stockStatus, string search, int page = 1)
        {
            var inventoryQuery = db.Inventories
                .Include(i => i.ProductVariant)
                .Include(i => i.ProductVariant.Product)
                .Include(i => i.ProductVariant.Product.Category)
                .Include(i => i.ProductVariant.Product.Brand)
                .Include(i => i.ProductVariant.Product.ProductImages)
                .Include(i => i.ProductVariant.Color)
                .Include(i => i.ProductVariant.Size)
                .Where(i => i.ProductVariant.Product.IsActive);

            // Apply filters
            if (categoryId.HasValue)
            {
                inventoryQuery = inventoryQuery.Where(i => i.ProductVariant.Product.CategoryID == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                inventoryQuery = inventoryQuery.Where(i => i.ProductVariant.Product.BrandID == brandId.Value);
            }

            // Filter by stock status - tính toán trong memory
            IQueryable<Inventory> filteredQuery = inventoryQuery;

            if (!string.IsNullOrEmpty(search))
            {
                filteredQuery = filteredQuery.Where(i =>
                    i.ProductVariant.Product.ProductName.Contains(search) ||
                    i.ProductVariant.SKU.Contains(search) ||
                    i.ProductVariant.Color.ColorName.Contains(search) ||
                    i.ProductVariant.Size.SizeName.Contains(search));
            }

            // Get data from database first
            var allInventoryData = filteredQuery.ToList();

            // Apply stock status filter in memory
            if (!string.IsNullOrEmpty(stockStatus))
            {
                switch (stockStatus.ToLower())
                {
                    case "outofstock":
                        allInventoryData = allInventoryData.Where(i => i.QuantityOnHand == 0).ToList();
                        break;
                    case "lowstock":
                        allInventoryData = allInventoryData.Where(i => (i.QuantityOnHand - i.QuantityReserved) > 0 && (i.QuantityOnHand - i.QuantityReserved) <= LOW_STOCK_THRESHOLD).ToList();
                        break;
                    case "reserved":
                        allInventoryData = allInventoryData.Where(i => i.QuantityReserved > 0).ToList();
                        break;
                    case "instock":
                        allInventoryData = allInventoryData.Where(i => (i.QuantityOnHand - i.QuantityReserved) > LOW_STOCK_THRESHOLD).ToList();
                        break;
                }
            }

            // Dashboard Statistics
            var allInv = db.Inventories
                .Include(i => i.ProductVariant)
                .Include(i => i.ProductVariant.Product)
                .Where(i => i.ProductVariant.Product.IsActive)
                .ToList();

            ViewBag.TotalVariants = allInv.Count;
            ViewBag.TotalQuantityOnHand = allInv.Sum(i => i.QuantityOnHand);
            ViewBag.TotalQuantityReserved = allInv.Sum(i => i.QuantityReserved);
            ViewBag.TotalAvailable = allInv.Sum(i => i.QuantityOnHand - i.QuantityReserved); // Tính trực tiếp
            ViewBag.LowStockVariants = allInv.Count(i =>
                (i.QuantityOnHand - i.QuantityReserved) > 0 &&
                (i.QuantityOnHand - i.QuantityReserved) <= LOW_STOCK_THRESHOLD
            );
            ViewBag.OutOfStockVariants = allInv.Count(i => i.QuantityOnHand == 0);
            ViewBag.TotalInventoryValue = allInv.Sum(i => i.QuantityOnHand * i.ProductVariant.Price);


            // Category statistics
            var categoryStats = db.Inventories
                .Include(i => i.ProductVariant.Product.Category)
                .Where(i => i.ProductVariant.Product.IsActive && i.ProductVariant.Product.Category != null)
                .ToList() // Load to memory
                .GroupBy(i => i.ProductVariant.Product.Category.CategoryName)
                .Select(g => new CategoryInventoryStats
                {
                    CategoryName = g.Key,
                    TotalQuantity = g.Sum(i => i.QuantityOnHand),
                    VariantCount = g.Count(),
                    ProductCount = g.Select(i => i.ProductVariant.ProductID).Distinct().Count()
                })
                .OrderByDescending(c => c.TotalQuantity)
                .Take(5)
                .ToList();
            ViewBag.CategoryStats = categoryStats;

            // Brand statistics
            var brandStats = db.Inventories
                .Include(i => i.ProductVariant.Product.Brand)
                .Where(i => i.ProductVariant.Product.IsActive && i.ProductVariant.Product.Brand != null)
                .ToList() // Load to memory
                .GroupBy(i => i.ProductVariant.Product.Brand.BrandName)
                .Select(g => new BrandInventoryStats
                {
                    BrandName = g.Key,
                    TotalQuantity = g.Sum(i => i.QuantityOnHand),
                    VariantCount = g.Count(),
                    ProductCount = g.Select(i => i.ProductVariant.ProductID).Distinct().Count()
                })
                .OrderByDescending(b => b.TotalQuantity)
                .Take(5)
                .ToList();
            ViewBag.BrandStats = brandStats;

            // Pagination
            int pageSize = 20;
            int totalItems = allInventoryData.Count;
            var pagedInventory = allInventoryData
                .OrderBy(i => i.ProductVariant.Product.ProductName)
                .ThenBy(i => i.ProductVariant.Color.ColorName)
                .ThenBy(i => i.ProductVariant.Size.SizeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Convert to ViewModel
            var inventoryViewModels = pagedInventory.Select(i => new InventoryViewModel
            {
                InventoryID = i.InventoryID,
                VariantID = i.VariantID,
                ProductID = i.ProductVariant.ProductID,
                ProductName = i.ProductVariant.Product.ProductName,
                CategoryName = i.ProductVariant.Product.Category?.CategoryName ?? "N/A",
                BrandName = i.ProductVariant.Product.Brand?.BrandName ?? "N/A",
                VariantSKU = i.ProductVariant.SKU,
                VariantColor = i.ProductVariant.Color.ColorName,
                VariantSize = i.ProductVariant.Size.SizeName,
                VariantPrice = i.ProductVariant.Price,
                QuantityOnHand = i.QuantityOnHand,
                QuantityReserved = i.QuantityReserved,
                AvailableQuantity = i.AvailableQuantity,
                StockStatus = GetStockStatus(i.QuantityOnHand, i.AvailableQuantity),
                ImageURL = i.ProductVariant.Product.ProductImages.FirstOrDefault()?.ImageURL ?? "/Content/images/no-image.png",
                UpdatedAt = i.UpdatedAt
            }).ToList();

            // Dropdown data
            ViewBag.Categories = new SelectList(
                db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                categoryId);

            ViewBag.Brands = new SelectList(
                db.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName),
                "BrandID",
                "BrandName",
                brandId);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentBrandId = brandId;
            ViewBag.CurrentStockStatus = stockStatus;
            ViewBag.CurrentSearch = search;

            return View(inventoryViewModels);
        }

        private string GetStockStatus(int quantityOnHand, int availableQuantity)
        {
            if (quantityOnHand == 0)
                return "Hết hàng";
            if (availableQuantity <= 0)
                return "Đã đặt hết";
            if (availableQuantity <= LOW_STOCK_THRESHOLD)
                return "Sắp hết";
            return "Còn hàng";
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
