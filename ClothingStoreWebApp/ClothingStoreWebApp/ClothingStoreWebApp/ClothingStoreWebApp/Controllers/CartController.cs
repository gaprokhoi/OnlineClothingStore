using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;

namespace ClothingStoreWebApp.Controllers
{
    
    /// CartController - Complete Shopping Cart Management
   
    public class CartController : Controller
    {
       
        /// Database context for all cart operations
       
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        /// GET: Cart/Index - Display user's shopping cart
       
        public ActionResult Index()
        {
            // Authentication check - cart requires logged-in user
            if (Session["UserID"] == null)
            {
                // Store current URL to return here after login
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("Index", "Cart") });
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Get all cart items for this user with full product details
               
                var cartItems = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Include(c => c.ProductVariant.Product)
                    .Include(c => c.ProductVariant.Color)
                    .Include(c => c.ProductVariant.Size)
                    .Include(c => c.User)
                    .OrderBy(c => c.AddedAt)
                    .ToList();

                return View(cartItems);
            }
            catch (Exception ex)
            {
                // Log error and show user-friendly message
                TempData["ErrorMessage"] = "Unable to load cart. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// POST: Cart/AddToCart - Add product variant to cart

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int? productId, int? variantId, int quantity = 1)
        {
            #region Authentication & Validation
            // Check user authentication
            if (Session["UserID"] == null)
            {
                var loginMessage = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = loginMessage });
                TempData["ErrorMessage"] = loginMessage;
                return RedirectToAction("Login", "User");
            }

            // Validate quantity
            if (quantity <= 0)
            {
                var quantityMessage = "Số lượng phải lớn hơn 0";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = quantityMessage });
                TempData["ErrorMessage"] = quantityMessage;
                return RedirectToAction("Index", "Product");
            }

            // Must have either productId or variantId
            if (!productId.HasValue && !variantId.HasValue)
            {
                var paramMessage = "Thiếu thông tin sản phẩm";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = paramMessage });
                TempData["ErrorMessage"] = paramMessage;
                return RedirectToAction("Index", "Product");
            }
        

            try
            {
                int userId = (int)Session["UserID"];
                ProductVariant variant = null;

                if (variantId.HasValue)
                {
                    // Direct variant selection
                    variant = db.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefault(v => v.VariantID == variantId && v.IsActive && v.Product.IsActive);
                }
                else if (productId.HasValue)
                {
                    // Find default variant for product or create a simple cart entry
                    var product = db.Products.FirstOrDefault(p => p.ProductID == productId && p.IsActive);
                    if (product == null)
                    {
                        var notFoundMessage = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa";
                        if (Request.IsAjaxRequest())
                            return Json(new { success = false, message = notFoundMessage });
                        TempData["ErrorMessage"] = notFoundMessage;
                        return RedirectToAction("Index", "Product");
                    }

                    // Try to find a default variant or the first available variant
                    variant = db.ProductVariants
                        .Include(v => v.Product)
                        .Where(v => v.ProductID == productId && v.IsActive)
                        .OrderBy(v => v.VariantID) // Get the first variant
                        .FirstOrDefault();

                    // If no variants exist, we'll create a simple cart entry using the base product
                    if (variant == null)
                    {
                        // For products without variants, add directly to cart
                        return AddProductWithoutVariant(userId, product, quantity);
                    }
                }

                if (variant == null)
                {
                    var notFoundMessage = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa";
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = notFoundMessage });
                    TempData["ErrorMessage"] = notFoundMessage;
                    return RedirectToAction("Index", "Product");
                }
                #endregion

                #region Inventory Check (Optional - skip if no inventory system)
                // Skip inventory check for now since you might not have Inventories table
                /*
                var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == variant.VariantID);
                int availableQuantity = inventory?.QuantityOnHand - inventory?.QuantityReserved ?? 999; // Default to high number if no inventory

                if (availableQuantity < quantity)
                {
                    var stockMessage = $"Chỉ còn {availableQuantity} sản phẩm trong kho";
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = stockMessage });
                    TempData["ErrorMessage"] = stockMessage;
                    return RedirectToAction("Details", "Product", new { id = variant.ProductID });
                }
                */
                #endregion

                #region Add or Update Cart Item
                // Check if item already exists in cart
                var existingCartItem = db.ShoppingCarts
                    .FirstOrDefault(c => c.UserID == userId && c.VariantID == variant.VariantID);

                if (existingCartItem != null)
                {
                    // Update existing item
                    existingCartItem.Quantity += quantity;
                    existingCartItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Create new cart item
                    var cartItem = new ShoppingCart
                    {
                        UserID = userId,
                        VariantID = variant.VariantID,
                        Quantity = quantity,
                        AddedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    db.ShoppingCarts.Add(cartItem);
                }

                // Save changes to database
                db.SaveChanges();
                #endregion

                #region Success Response
                // Get updated cart count
                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                var successMessage = $"Đã thêm \"{variant.Product.ProductName}\" vào giỏ hàng";

                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        cartCount = cartCount
                    });
                }

                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction("Index");
                #endregion
            }
            catch (Exception ex)
            {
                var errorMessage = "Không thể thêm sản phẩm vào giỏ hàng. Vui lòng thử lại!";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = errorMessage });
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Product");
            }
        }


        /// Helper method to add product without variants

        private ActionResult AddProductWithoutVariant(int userId, Product product, int quantity)
        {
            try
            {
                // Check if we already have a default variant for this product
                var existingDefaultVariant = db.ProductVariants
                    .FirstOrDefault(v => v.ProductID == product.ProductID && v.IsActive);

                if (existingDefaultVariant != null)
                {
                    // Use existing default variant
                    var existingCartItem = db.ShoppingCarts
                        .FirstOrDefault(c => c.UserID == userId && c.VariantID == existingDefaultVariant.VariantID);

                    if (existingCartItem != null)
                    {
                        existingCartItem.Quantity += quantity;
                        existingCartItem.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        var cartItem = new ShoppingCart
                        {
                            UserID = userId,
                            VariantID = existingDefaultVariant.VariantID,
                            Quantity = quantity,
                            AddedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        db.ShoppingCarts.Add(cartItem);
                    }

                    db.SaveChanges();

                    var successMessage = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng";
                    if (Request.IsAjaxRequest())
                    {
                        int cartCount = db.ShoppingCarts.Where(c => c.UserID == userId).Sum(c => (int?)c.Quantity) ?? 0;
                        return Json(new { success = true, message = successMessage, cartCount = cartCount });
                    }

                    TempData["SuccessMessage"] = successMessage;
                    return RedirectToAction("Index");
                }

                // Create default Size if it doesn't exist
                var defaultSize = db.Sizes.FirstOrDefault(s => s.SizeName == "One Size" && s.IsActive);
                if (defaultSize == null)
                {
                    defaultSize = new Size
                    {
                        SizeName = "One Size",
                        SizeOrder = 0,
                        Category = "Default", // Required field in your model
                        IsActive = true
                    };
                    db.Sizes.Add(defaultSize);
                    db.SaveChanges(); // Save to get ID
                }

                // Create default Color if it doesn't exist  
                var defaultColor = db.Colors.FirstOrDefault(c => c.ColorName == "Default" && c.IsActive);
                if (defaultColor == null)
                {
                    defaultColor = new Color
                    {
                        ColorName = "Default",
                        ColorCode = "#FFFFFF", // Required field in your model
                        IsActive = true
                    };
                    db.Colors.Add(defaultColor);
                    db.SaveChanges(); // Save to get ID
                }

                // Generate unique SKU
                string baseSku = product.ProductName.Replace(" ", "").ToUpper();
                string sku = baseSku.Length > 90 ? baseSku.Substring(0, 90) : baseSku;
                sku += "_DEF"; // Add suffix to ensure uniqueness

                // Check if SKU already exists and make it unique
                int counter = 1;
                string originalSku = sku;
                while (db.ProductVariants.Any(pv => pv.SKU == sku))
                {
                    sku = originalSku + counter.ToString();
                    if (sku.Length > 100) // SKU max length is 100
                    {
                        sku = originalSku.Substring(0, 95) + counter.ToString();
                    }
                    counter++;
                }

                // Now create the default variant with required ColorID and SizeID
                var defaultVariant = new ProductVariant
                {
                    ProductID = product.ProductID,
                    SKU = sku,
                    ColorID = defaultColor.ColorID,
                    SizeID = defaultSize.SizeID,
                    Price = product.BasePrice,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                db.ProductVariants.Add(defaultVariant);
                db.SaveChanges();

                // Now add to cart with this variant
                var newCartItem = new ShoppingCart
                {
                    UserID = userId,
                    VariantID = defaultVariant.VariantID,
                    Quantity = quantity,
                    AddedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.ShoppingCarts.Add(newCartItem);
                db.SaveChanges();

                var finalSuccessMessage = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng";
                if (Request.IsAjaxRequest())
                {
                    int cartCount = db.ShoppingCarts.Where(c => c.UserID == userId).Sum(c => (int?)c.Quantity) ?? 0;
                    return Json(new { success = true, message = finalSuccessMessage, cartCount = cartCount });
                }

                TempData["SuccessMessage"] = finalSuccessMessage;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                System.Diagnostics.Debug.WriteLine($"AddProductWithoutVariant Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                var errorMessage = $"Lỗi khi thêm sản phẩm vào giỏ hàng: {ex.Message}";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = errorMessage });
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Product");
            }
        }

        /// POST: Cart/UpdateQuantity - Update cart item quantity

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int cartId, int quantity)
        {
            // Authentication check
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Please login to update cart" });

            // Validate quantity
            if (quantity <= 0)
                return Json(new { success = false, message = "Quantity must be greater than 0" });

            try
            {
                int userId = (int)Session["UserID"];

                // Find cart item - FIXED: Using correct DbSet name
                var cartItem = db.ShoppingCarts
                    .Include(c => c.ProductVariant)
                    .FirstOrDefault(c => c.CartID == cartId && c.UserID == userId);

                if (cartItem == null)
                    return Json(new { success = false, message = "Cart item not found" });

                // Check inventory - FIXED: Using correct DbSet name
                var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == cartItem.VariantID);
                int availableQuantity = inventory?.QuantityOnHand - inventory?.QuantityReserved ?? 0;

                if (quantity > availableQuantity)
                    return Json(new { success = false, message = $"Only {availableQuantity} items available in stock" });

                // Update quantity
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // Calculate totals - FIXED: Handle null Sum()
                decimal itemTotal = cartItem.Quantity * cartItem.ProductVariant.Price;

                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                decimal cartTotal = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Sum(c => (decimal?)(c.Quantity * c.ProductVariant.Price)) ?? 0;

                return Json(new
                {
                    success = true,
                    message = "Cart updated successfully",
                    itemTotal = itemTotal,
                    cartCount = cartCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to update cart. Please try again." });
            }
        }

       

       
        /// POST: Cart/RemoveItem - Remove item from cart
    
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int cartId)
        {
            // Authentication check
            if (Session["UserID"] == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Please login to remove items" });
                return RedirectToAction("Login", "User");
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Find and validate cart item - FIXED: Using correct DbSet name
                var cartItem = db.ShoppingCarts
                    .FirstOrDefault(c => c.CartID == cartId && c.UserID == userId);

                if (cartItem == null)
                {
                    var notFoundMessage = "Cart item not found";
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = notFoundMessage });

                    TempData["ErrorMessage"] = notFoundMessage;
                    return RedirectToAction("Index");
                }

                // Remove item - FIXED: Using correct DbSet name
                db.ShoppingCarts.Remove(cartItem);
                db.SaveChanges();

                // Calculate updated totals - FIXED: Handle null Sum()
                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                decimal cartTotal = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Sum(c => (decimal?)(c.Quantity * c.ProductVariant.Price)) ?? 0;

                var successMessage = "Item removed from cart";

                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        cartCount = cartCount,
                        cartTotal = cartTotal
                    });
                }

                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var errorMessage = "Unable to remove item. Please try again.";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = errorMessage });

                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }
        }


       
        /// POST: Cart/ClearCart - Remove all items from cart
        /// Purpose: Empty user's entire cart
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            // Authentication check
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "User");

            try
            {
                int userId = (int)Session["UserID"];

                
                var cartItems = db.ShoppingCarts.Where(c => c.UserID == userId).ToList();

                if (cartItems.Any())
                {
                    // Remove all items - FIXED: Using correct DbSet name
                    db.ShoppingCarts.RemoveRange(cartItems);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cart cleared successfully!";
                }
                else
                {
                    TempData["InfoMessage"] = "Cart is already empty";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to clear cart. Please try again.";
            }

            return RedirectToAction("Index");
        }

      

       
        /// GET: Cart/GetCartCount - Get cart item count for navigation
        /// Purpose: AJAX endpoint to update cart badge in navbar
    
        public ActionResult GetCartCount()
        {
            try
            {
                // Return 0 for non-authenticated users
                if (Session["UserID"] == null)
                    return Json(new { cartCount = 0 }, JsonRequestBehavior.AllowGet);

                int userId = (int)Session["UserID"];

                // FIXED: Handle null Sum() by casting to nullable int
                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                return Json(new { cartCount = cartCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Return 0 on any error to prevent breaking navbar
                return Json(new { cartCount = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

     

        

       
        /// Clean up database context
        /// Purpose: Properly dispose of resources
     
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

       
    }
}
