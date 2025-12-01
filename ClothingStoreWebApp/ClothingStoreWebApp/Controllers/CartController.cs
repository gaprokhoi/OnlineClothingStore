using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Models;

namespace ClothingStoreWebApp.Controllers
{
    public class CartController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();

        // GET: Cart/Index - Display user's shopping cart
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("Index", "Cart") });
            }

            try
            {
                int userId = (int)Session["UserID"];

                var cartItems = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Include(c => c.ProductVariant.Color)
                    .Include(c => c.ProductVariant.Size)
                    .Include(c => c.ProductVariant.Product)
                    .Include(c => c.ProductVariant.Product.Brand)
                    .Include(c => c.ProductVariant.Product.ProductImages)
                    .Include(c => c.ProductVariant.Product.ProductVariants)
                    .Include(c => c.ProductVariant.Product.ProductVariants.Select(v => v.Color))
                    .Include(c => c.ProductVariant.Product.ProductVariants.Select(v => v.Size))
                    // ✅ KHÔNG cần Include Inventory vì View không dùng
                    .Include(c => c.User)
                    .OrderByDescending(c => c.AddedAt)
                    .ToList();

                // Calculate totals
                decimal subTotal = cartItems.Sum(c => c.Quantity * c.ProductVariant.Price);
                int cartCount = cartItems.Sum(c => c.Quantity);
                Session["CartCount"] = cartCount;

                ViewBag.SubTotal = subTotal;
                ViewBag.CartCount = cartItems.Count;

                return View(cartItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cart Index Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "Lỗi! Không thể tải giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }


        // Action to update the selected variant (Color/Size)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateVariant(int cartId, int newColorId, int newSizeId)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thay đổi" });
            }

            try
            {
                int userId = (int)Session["UserID"];

                // 1. Get the original cart item
                var oldCartItem = db.ShoppingCarts
                    .Include(c => c.ProductVariant)
                    .FirstOrDefault(c => c.CartID == cartId && c.UserID == userId);

                if (oldCartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                int productId = oldCartItem.ProductVariant.ProductID;
                int currentQuantity = oldCartItem.Quantity;

                // 2. Find the new variant
                var newVariant = db.ProductVariants
                    .FirstOrDefault(v => v.ProductID == productId &&
                                         v.ColorID == newColorId &&
                                         v.SizeID == newSizeId &&
                                         v.IsActive);

                if (newVariant == null)
                {
                    return Json(new { success = false, message = "Tổ hợp màu sắc/kích thước này không có sẵn." });
                }

                // 3. If the new variant is the same as the old one
                if (newVariant.VariantID == oldCartItem.VariantID)
                {
                    var currentInventory = db.Inventories.FirstOrDefault(i => i.VariantID == oldCartItem.VariantID);

                    // ✅ SỬA
                    int available = currentInventory != null
                        ? (currentInventory.QuantityOnHand - currentInventory.QuantityReserved)
                        : int.MaxValue;

                    if (currentInventory != null && currentQuantity > available)
                    {
                        oldCartItem.Quantity = available;
                        db.SaveChanges();
                        return Json(new
                        {
                            success = false,
                            message = $"Số lượng đã được cập nhật thành {available} do tồn kho thay đổi.",
                            reload = true
                        });
                    }

                    return Json(new { success = true, message = "Lựa chọn không thay đổi." });
                }

                // 4. Check inventory for the NEW variant
                var newInventory = db.Inventories.FirstOrDefault(i => i.VariantID == newVariant.VariantID);

                // ✅ SỬA
                int newAvailable = newInventory != null
                    ? (newInventory.QuantityOnHand - newInventory.QuantityReserved)
                    : int.MaxValue;

                if (newInventory != null && currentQuantity > newAvailable)
                {
                    // ✅ SỬA - Dùng newAvailable thay vì .AvailableQuantity
                    return Json(new
                    {
                        success = false,
                        message = $"Sản phẩm với lựa chọn mới chỉ còn {newAvailable} trong kho. Vui lòng giảm số lượng trước khi đổi."
                    });
                }

                // 5. Check if the NEW variant already exists in cart
                var existingCartItem = db.ShoppingCarts
                    .FirstOrDefault(c => c.UserID == userId &&
                                         c.VariantID == newVariant.VariantID &&
                                         c.CartID != oldCartItem.CartID);

                if (existingCartItem != null)
                {
                    int totalQuantity = existingCartItem.Quantity + currentQuantity;

                    // ✅ SỬA - Dùng newAvailable
                    if (newInventory != null && totalQuantity > newAvailable)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Không thể gộp sản phẩm, chỉ còn {newAvailable} sản phẩm trong kho cho lựa chọn mới."
                        });
                    }

                    existingCartItem.Quantity = totalQuantity;
                    existingCartItem.UpdatedAt = DateTime.Now;
                    db.ShoppingCarts.Remove(oldCartItem);
                }
                else
                {
                    oldCartItem.VariantID = newVariant.VariantID;
                    oldCartItem.UpdatedAt = DateTime.Now;
                }

                db.SaveChanges();

                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;
                Session["CartCount"] = cartCount;

                decimal cartTotal = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Sum(c => (decimal?)(c.Quantity * c.ProductVariant.Price)) ?? 0;

                return Json(new
                {
                    success = true,
                    message = "Cập nhật lựa chọn thành công.",
                    reload = true,
                    cartCount = cartCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateVariant Error: {ex.ToString()}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật sản phẩm." });
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int variantId, int quantity = 1)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Check if variant exists and is active
                var variant = db.ProductVariants
                    .Include(v => v.Product) // Include product for name in message
                    .FirstOrDefault(v => v.VariantID == variantId && v.IsActive && v.Product.IsActive);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Sản phẩm hoặc phiên bản này không tồn tại hoặc đã ngừng bán." });
                }

                // Check inventory availability
                var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == variantId);
                int availableQuantity = inventory != null
                    ? (inventory.QuantityOnHand - inventory.QuantityReserved)
                    : int.MaxValue;

                // Check existing cart quantity
                var existingCartItem = db.ShoppingCarts
                    .FirstOrDefault(c => c.UserID == userId && c.VariantID == variantId);

                int currentCartQuantity = existingCartItem?.Quantity ?? 0;
                int totalRequestedQuantity = quantity + currentCartQuantity;

                if (totalRequestedQuantity > availableQuantity)
                {
                    // Adjust quantity if possible, otherwise return error
                    if (availableQuantity - currentCartQuantity <= 0)
                    {
                        return Json(new { success = false, message = $"Sản phẩm \"{variant.Product.ProductName}\" đã hết hàng hoặc số lượng trong giỏ đã tối đa." });
                    }
                    else
                    {
                        quantity = availableQuantity - currentCartQuantity; // Add only available amount
                        totalRequestedQuantity = availableQuantity;
                        // Inform user quantity was adjusted? Optional.
                    }
                }

                // If quantity was adjusted to 0 or less (shouldn't happen with check above, but safeguard)
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Không thể thêm số lượng này vào giỏ hàng." });
                }


                if (existingCartItem != null)
                {
                    // Update existing cart item quantity
                    existingCartItem.Quantity += quantity;
                    existingCartItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Add new cart item
                    var cartItem = new ShoppingCart
                    {
                        UserID = userId,
                        VariantID = variantId,
                        Quantity = quantity,
                        AddedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    db.ShoppingCarts.Add(cartItem);
                }

                db.SaveChanges();

                // Recalculate cart count
                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;
                Session["CartCount"] = cartCount; // Update session

                var successMessage = $"Đã thêm {quantity} x \"{variant.Product.ProductName}\" vào giỏ hàng";
                if (totalRequestedQuantity == availableQuantity && inventory != null)
                {
                    successMessage += $" (Đã đạt số lượng tối đa trong kho)";
                }

                return Json(new { success = true, message = successMessage, cartCount = cartCount });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddToCart Error: {ex.ToString()}");
                return Json(new { success = false, message = "Không thể thêm vào giỏ hàng. Vui lòng thử lại." });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int cartId, int quantity)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để cập nhật giỏ hàng" });

            if (quantity <= 0)
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });

            try
            {
                int userId = (int)Session["UserID"];

                // Find cart item, include variant for price and inventory check
                var cartItem = db.ShoppingCarts
                    .Include(c => c.ProductVariant)
                    .FirstOrDefault(c => c.CartID == cartId && c.UserID == userId);

                if (cartItem == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });

                // Check inventory
                var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == cartItem.VariantID);
                int availableQuantity = inventory != null
                    ? (inventory.QuantityOnHand - inventory.QuantityReserved)
                    : int.MaxValue;

                if (quantity > availableQuantity)
                {
                    // Optionally adjust quantity to max available instead of erroring out
                    // quantity = availableQuantity;
                    // TempData["InfoMessage"] = $"Số lượng đã được điều chỉnh thành {quantity} do giới hạn tồn kho.";
                    return Json(new { success = false, message = $"Chỉ còn {availableQuantity} sản phẩm trong kho" });
                }

                // Update quantity and timestamp
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                // Calculate totals after update
                decimal itemTotal = cartItem.Quantity * cartItem.ProductVariant.Price;

                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                decimal cartTotal = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant) // Include needed here too
                    .Sum(c => (decimal?)(c.Quantity * c.ProductVariant.Price)) ?? 0;

                // Update Session Cart Count
                Session["CartCount"] = cartCount;

                return Json(new
                {
                    success = true,
                    message = "Cập nhật giỏ hàng thành công",
                    itemTotal = itemTotal,
                    cartCount = cartCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateQuantity Error: {ex.ToString()}");
                return Json(new { success = false, message = "Không thể cập nhật giỏ hàng. Vui lòng thử lại." });
            }
        }

        // --- ACTION NAME CORRECTED ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int cartId)
        {
            if (Session["UserID"] == null)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Vui lòng đăng nhập để xóa sản phẩm" });
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("Index", "Cart") });
            }

            try
            {
                int userId = (int)Session["UserID"];

                var cartItem = db.ShoppingCarts
                    .FirstOrDefault(c => c.CartID == cartId && c.UserID == userId);

                if (cartItem == null)
                {
                    var notFoundMessage = "Không tìm thấy sản phẩm trong giỏ hàng";
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = notFoundMessage });

                    TempData["ErrorMessage"] = notFoundMessage;
                    return RedirectToAction("Index");
                }

                db.ShoppingCarts.Remove(cartItem);
                db.SaveChanges(); // Save changes BEFORE recalculating totals

                // --- RECALCULATE TOTALS ---
                int cartCount = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;

                // --- INCLUDE ADDED HERE ---
                decimal cartTotal = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant) // <-- CORRECTED: Include added
                    .Sum(c => (decimal?)(c.Quantity * c.ProductVariant.Price)) ?? 0;

                // Update Session Cart Count
                Session["CartCount"] = cartCount;

                var successMessage = "Đã xóa sản phẩm khỏi giỏ hàng";

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
                System.Diagnostics.Debug.WriteLine($"RemoveFromCart Error: {ex.ToString()}");
                var errorMessage = "Không thể xóa sản phẩm. Vui lòng thử lại.";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = errorMessage });

                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("Index", "Cart") });


            try
            {
                int userId = (int)Session["UserID"];

                var cartItems = db.ShoppingCarts.Where(c => c.UserID == userId).ToList();

                if (cartItems.Any())
                {
                    db.ShoppingCarts.RemoveRange(cartItems);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng!";
                }
                else
                {
                    // Use TempData["InfoMessage"] or similar if you want to distinguish
                    TempData["SuccessMessage"] = "Giỏ hàng của bạn đã trống.";
                }

                Session["CartCount"] = 0; // Reset session explicitly
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearCart Error: {ex.ToString()}");
                TempData["ErrorMessage"] = "Không thể xóa giỏ hàng. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }


        public ActionResult GetCartCount()
        {
            int cartCount = 0; // Default to 0
            try
            {
                if (Session["UserID"] != null)
                {
                    int userId = (int)Session["UserID"];
                    // Use nullable Sum to handle empty cart correctly
                    cartCount = db.ShoppingCarts
                        .Where(c => c.UserID == userId)
                        .Sum(c => (int?)c.Quantity) ?? 0;

                    // Sync session just in case
                    Session["CartCount"] = cartCount;
                }
                else
                {
                    // Ensure session is 0 if user ID is null
                    Session["CartCount"] = 0;
                }
            }
            catch (Exception ex)
            {
                // Log error but still return 0 to avoid breaking UI
                System.Diagnostics.Debug.WriteLine($"GetCartCount Error: {ex.ToString()}");
                cartCount = 0;
                Session["CartCount"] = 0; // Reset session on error too
            }
            // Always return JSON, even if count is 0
            return Json(new { cartCount = cartCount }, JsonRequestBehavior.AllowGet);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

        // AddProductWithoutVariant method would go here if you had it...
    }
}