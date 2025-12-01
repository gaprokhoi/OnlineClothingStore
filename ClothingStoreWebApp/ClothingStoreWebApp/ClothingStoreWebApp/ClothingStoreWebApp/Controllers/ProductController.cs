using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Helpers;
using ClothingStoreWebApp.Models;
using System;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ClothingStoreWebApp.Controllers
{
    public class ProductController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        #region Public Product Views (Customer-facing)

        // GET: Product - Public access cho khách hàng
        public ActionResult Index(int? categoryId, int? brandId, string searchTerm, decimal? minPrice, decimal? maxPrice, string sortBy = "name", int page = 1)
        {
            var products = db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Collection)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive);

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
                products = products.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.BasePrice <= maxPrice.Value);
            }

            // Apply sorting
            switch (sortBy?.ToLower())
            {
                case "price_asc":
                    products = products.OrderBy(p => p.BasePrice);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.BasePrice);
                    break;
                case "newest":
                    products = products.OrderByDescending(p => p.CreatedAt);
                    break;
                case "featured":
                    products = products.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.ProductName);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            // Pagination
            int pageSize = 12;
            var totalItems = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag data for filters
            ViewBag.CategoryId = new SelectList(db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName), "CategoryID", "CategoryName", categoryId);
            ViewBag.BrandId = new SelectList(db.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName), "BrandID", "BrandName", brandId);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentFilters = new
            {
                categoryId,
                brandId,
                searchTerm,
                minPrice,
                maxPrice,
                sortBy
            };

            return View(pagedProducts);
        }

        // GET: Product/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Collection)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants.Select(pv => pv.Size))
                .Include(p => p.ProductVariants.Select(pv => pv.Color))
                .FirstOrDefault(p => p.ProductID == id && p.IsActive);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Get available sizes and colors for this product
            var availableSizes = product.ProductVariants
                .Where(pv => pv.IsActive)
                .Select(pv => pv.Size)
                .Distinct()
                .OrderBy(s => s.SizeOrder)
                .ToList();

            var availableColors = product.ProductVariants
                .Where(pv => pv.IsActive)
                .Select(pv => pv.Color)
                .Distinct()
                .OrderBy(c => c.ColorName)
                .ToList();

            ViewBag.AvailableSizes = availableSizes;
            ViewBag.AvailableColors = availableColors;

            // Get related products
            var relatedProducts = db.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && p.ProductID != id &&
                       (p.CategoryID == product.CategoryID || p.BrandID == product.BrandID))
                .OrderBy(p => Guid.NewGuid())
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        #endregion

        #region Admin Product Management

        // GET: Product/Create (Admin only)
        [HttpGet]
        public ActionResult Create()
        {
            PopulateDropDownLists();
            PopulateSizeColorLists();
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductName,Description,CategoryID,BrandID,CollectionID,Material,CareInstructions,Gender,BasePrice,IsActive,IsFeatured")] Product product,
            HttpPostedFileBase[] productImages,
            int[] SelectedSizes,
            int[] SelectedColors)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set timestamps
                    product.CreatedAt = DateTime.Now;
                    product.UpdatedAt = DateTime.Now;

                    // Save product first to get ProductID
                    db.Products.Add(product);
                    db.SaveChanges();

                    // Handle image uploads
                    if (productImages != null && productImages.Any(img => img != null))
                    {
                        ProcessProductImages(product.ProductID, productImages);
                    }

                    // Create ProductVariants
                    CreateProductVariants(product.ProductID, product.BasePrice, SelectedSizes, SelectedColors);

                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
                    return RedirectToAction("Index", "Admin");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            // Repopulate dropdown lists if validation fails
            PopulateDropDownLists(product);
            PopulateSizeColorLists();
            return View(product);
        }

        // GET: Product/Edit/5 (Admin only)
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants.Select(pv => pv.Size))
                .Include(p => p.ProductVariants.Select(pv => pv.Color))
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            PopulateDropDownLists(product);
            PopulateSizeColorLists(id);

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,Description,CategoryID,BrandID,CollectionID,Material,CareInstructions,Gender,BasePrice,IsActive,IsFeatured,CreatedAt")] Product product,
            HttpPostedFileBase[] productImages,
            int[] SelectedSizes,
            int[] SelectedColors)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Update product info
                    product.UpdatedAt = DateTime.Now;
                    db.Entry(product).State = EntityState.Modified;

                    // Update ProductVariants
                    UpdateProductVariants(product.ProductID, product.BasePrice, SelectedSizes, SelectedColors);

                    // Handle new image uploads
                    if (productImages != null && productImages.Any(img => img != null))
                    {
                        ProcessProductImages(product.ProductID, productImages);
                    }

                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction("Index", "Admin");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            PopulateDropDownLists(product);
            PopulateSizeColorLists(product.ProductID);
            return View(product);
        }

        // POST: Product/SoftDelete/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
            try
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    // Soft delete - just mark as inactive
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.Now;

                    // Also soft delete all variants
                    var variants = db.ProductVariants.Where(pv => pv.ProductID == id).ToList();
                    foreach (var variant in variants)
                    {
                        variant.IsActive = false;
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Sản phẩm đã được ẩn thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index", "Admin");
        }

        // POST: Product/Delete/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var product = db.Products.Find(id);
                if (product != null)
                {
                    // Soft delete
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index", "Admin");
        }
        // POST: Product/Restore/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(int id)
        {
            try
            {
                var product = db.Products
                    .Include(p => p.ProductVariants)
                    .FirstOrDefault(p => p.ProductID == id);

                if (product != null)
                {
                    // Restore product
                    product.IsActive = true;
                    product.UpdatedAt = DateTime.Now;

                    // Restore all variants
                    foreach (var variant in product.ProductVariants)
                    {
                        variant.IsActive = true;
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Sản phẩm '{product.ProductName}' đã được khôi phục thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi khôi phục sản phẩm: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"Restore Error: {ex.Message}");
            }

            return RedirectToAction("Index", "Admin");
        }
        #endregion

        #region AJAX Methods for Dynamic Loading

        // AJAX: Get variants for selected product
        [HttpGet]
        public JsonResult GetProductVariants(int productId)
        {
            try
            {
                var variants = db.ProductVariants
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .Where(pv => pv.ProductID == productId && pv.IsActive)
                    .Select(pv => new
                    {
                        variantId = pv.VariantID,
                        sizeId = pv.SizeID,
                        sizeName = pv.Size.SizeName,
                        colorId = pv.ColorID,
                        colorName = pv.Color.ColorName,
                        colorCode = pv.Color.ColorCode,
                        price = pv.Price,
                        sku = pv.SKU
                    })
                    .ToList();

                return Json(new { success = true, variants = variants }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // AJAX: Get variant by size and color
        [HttpGet]
        public JsonResult GetVariantByOptions(int productId, int sizeId, int colorId)
        {
            try
            {
                var variant = db.ProductVariants
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .FirstOrDefault(pv => pv.ProductID == productId &&
                                         pv.SizeID == sizeId &&
                                         pv.ColorID == colorId &&
                                         pv.IsActive);

                if (variant != null)
                {
                    return Json(new
                    {
                        success = true,
                        variant = new
                        {
                            variantId = variant.VariantID,
                            price = variant.Price,
                            sku = variant.SKU,
                            sizeName = variant.Size.SizeName,
                            colorName = variant.Color.ColorName
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = false, message = "Không tìm thấy variant phù hợp" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region ProductVariant Management Helper Methods

        /// <summary>
        /// Create ProductVariants for new product
        /// </summary>
        private void CreateProductVariants(int productId, decimal basePrice, int[] selectedSizes, int[] selectedColors)
        {
            try
            {
                if (selectedSizes != null && selectedColors != null && selectedSizes.Length > 0 && selectedColors.Length > 0)
                {
                    var product = db.Products.Find(productId);
                    string productName = product?.ProductName ?? "PRODUCT";

                    foreach (int sizeId in selectedSizes)
                    {
                        foreach (int colorId in selectedColors)
                        {
                            var variant = new ProductVariant
                            {
                                ProductID = productId,
                                SizeID = sizeId,
                                ColorID = colorId,
                                SKU = GenerateUniqueSKU(productName, sizeId, colorId),
                                Price = basePrice,
                                IsActive = true,
                                CreatedAt = DateTime.Now
                            };
                            db.ProductVariants.Add(variant);
                        }
                    }
                }
                else
                {
                    // Create default variant if no specific sizes/colors selected
                    CreateDefaultVariant(productId, basePrice);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating variants: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update ProductVariants for existing product
        /// </summary>
        private void UpdateProductVariants(int productId, decimal basePrice, int[] selectedSizes, int[] selectedColors)
        {
            try
            {
                // Remove existing variants
                var existingVariants = db.ProductVariants.Where(pv => pv.ProductID == productId).ToList();
                if (existingVariants.Any())
                {
                    db.ProductVariants.RemoveRange(existingVariants);
                }

                // Create new variants
                CreateProductVariants(productId, basePrice, selectedSizes, selectedColors);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating variants: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate unique SKU for ProductVariant
        /// </summary>
        private string GenerateUniqueSKU(string productName, int sizeId, int colorId)
        {
            // Clean product name for SKU
            string baseSku = productName
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "")
                .ToUpper();

            if (baseSku.Length > 50) baseSku = baseSku.Substring(0, 50);

            // Create SKU pattern: PRODUCTNAME_SIZEID_COLORID
            string sku = $"{baseSku}_S{sizeId}_C{colorId}";

            // Ensure uniqueness by adding counter if needed
            int counter = 1;
            string originalSku = sku;
            while (db.ProductVariants.Any(pv => pv.SKU == sku))
            {
                sku = $"{originalSku}_{counter}";
                counter++;
                if (sku.Length > 100 || counter > 999) break; // Prevent infinite loop
            }

            return sku.Length > 100 ? sku.Substring(0, 100) : sku;
        }

        /// <summary>
        /// Create default variant when no sizes/colors selected
        /// </summary>
        private void CreateDefaultVariant(int productId, decimal basePrice)
        {
            try
            {
                var defaultSize = db.Sizes.FirstOrDefault(s => s.SizeName == "One Size" && s.IsActive);
                var defaultColor = db.Colors.FirstOrDefault(c => c.ColorName == "Default" && c.IsActive);

                if (defaultSize != null && defaultColor != null)
                {
                    var variant = new ProductVariant
                    {
                        ProductID = productId,
                        SizeID = defaultSize.SizeID,
                        ColorID = defaultColor.ColorID,
                        SKU = GenerateUniqueSKU("DEFAULT_PRODUCT", defaultSize.SizeID, defaultColor.ColorID),
                        Price = basePrice,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    db.ProductVariants.Add(variant);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating default variant: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Populate dropdown lists for Create/Edit forms
        /// </summary>
        private void PopulateDropDownLists(Product product = null)
        {
            ViewBag.CategoryID = new SelectList(
                db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName),
                "CategoryID",
                "CategoryName",
                product?.CategoryID);

            ViewBag.BrandID = new SelectList(
                db.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName),
                "BrandID",
                "BrandName",
                product?.BrandID);

            ViewBag.CollectionID = new SelectList(
                db.SeasonalCollections.Where(c => c.IsActive).OrderBy(c => c.CollectionName),
                "CollectionID",
                "CollectionName",
                product?.CollectionID);
        }

        /// Populate Size and Color lists for Product management
        private void PopulateSizeColorLists(int? productId = null)
        {
            ViewBag.AllSizes = db.Sizes.Where(s => s.IsActive).OrderBy(s => s.SizeOrder).ToList();
            ViewBag.AllColors = db.Colors.Where(c => c.IsActive).OrderBy(c => c.ColorName).ToList();

            if (productId.HasValue)
            {
                // Get existing variants for editing
                var existingVariants = db.ProductVariants.Where(pv => pv.ProductID == productId.Value).ToList();
                ViewBag.SelectedSizes = existingVariants.Select(pv => pv.SizeID).Distinct().ToArray();
                ViewBag.SelectedColors = existingVariants.Select(pv => pv.ColorID).Distinct().ToArray();
            }
        }

        /// Process uploaded product images
       
        private void ProcessProductImages(int productId, HttpPostedFileBase[] images)
        {
            try
            {
                string uploadPath = Server.MapPath("~/Content/images/products/");

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                int sortOrder = 1;
                foreach (var image in images.Where(img => img != null && img.ContentLength > 0))
                {
                    // Validate image file types
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string extension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        continue;

                    // Generate unique filename
                    string fileName = $"product_{productId}_{DateTime.Now.Ticks}_{sortOrder}{Path.GetExtension(image.FileName)}";
                    string filePath = Path.Combine(uploadPath, fileName);

                    // Save image
                    image.SaveAs(filePath);

                    // Save to database - SỬ DỤNG TÊN PROPERTIES CHÍNH XÁC
                    var productImage = new ProductImage
                    {
                        ProductID = productId,
                        ImageURL = "/Content/images/products/" + fileName,      
                        SortOrder = sortOrder,                                  
                        ImageType = "Product",                                  
                        AltText = $"Product {productId} image {sortOrder}",     
                        CreatedAt = DateTime.Now,                              
                        ColorID = null                                         
                                                                                
                                                                               
                    };

                    db.ProductImages.Add(productImage);
                    sortOrder++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing images: {ex.Message}");
            }
        }


        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
