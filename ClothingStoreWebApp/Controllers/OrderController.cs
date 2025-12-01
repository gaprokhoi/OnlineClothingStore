using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Helpers;
using ClothingStoreWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ClothingStoreWebApp.Controllers
{
    public class OrderController : Controller
    {
        private ClothingStoreDbContext db = new ClothingStoreDbContext();
        // Helper method to initialize OrderStatus (call this once or create migration)
        private void InitializeOrderStatus()
        {
            var statuses = new[]
            {
                new OrderStatus { StatusName = "Pending", Description = "Order placed, awaiting processing", SortOrder = 1 },
                new OrderStatus { StatusName = "Processing", Description = "Order is being prepared", SortOrder = 2 },
                new OrderStatus { StatusName = "Shipped", Description = "Order has been shipped", SortOrder = 3 },
                new OrderStatus { StatusName = "Delivered", Description = "Order delivered to customer", SortOrder = 4 },
                new OrderStatus { StatusName = "Cancelled", Description = "Order cancelled", SortOrder = 99 }
            };

            foreach (var status in statuses)
            {
                if (!db.OrderStatus.Any(s => s.StatusName == status.StatusName))
                {
                    db.OrderStatus.Add(status);
                }
            }
            db.SaveChanges();
        }

        // GET: Order/Checkout
        public ActionResult Checkout()
        {
            // Authentication check
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán";
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("Checkout", "Order") });
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Get cart items with all necessary includes
                var cartItems = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant)
                    .Include(c => c.ProductVariant.Product)
                    .Include(c => c.ProductVariant.Color)
                    .Include(c => c.ProductVariant.Size)
                    .ToList();

                // Check if cart is empty
                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                // Calculate totals
                decimal subtotal = cartItems.Sum(c => c.Quantity * c.ProductVariant.Price);
                decimal shippingFee = 30000m; // Fixed shipping fee
                decimal taxAmount = 0m;
                decimal total = subtotal + shippingFee + taxAmount;

                // Get user info for pre-filling form
                var user = db.Users.Find(userId);

                // IMPORTANT: Pass data to view using ViewBag
                ViewBag.CartItems = cartItems;
                ViewBag.SubTotal = subtotal;
                ViewBag.ShippingFee = shippingFee;
                ViewBag.TaxAmount = taxAmount;
                ViewBag.Total = total;
                ViewBag.User = user ?? new User(); // Prevent null reference

                return View();
            }
            catch (Exception ex)
            {
                // Log detailed error
                System.Diagnostics.Debug.WriteLine("Checkout Error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Inner Exception: " + (ex.InnerException?.Message ?? "None"));
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                TempData["ErrorMessage"] = "Không thể tải trang thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }



        // POST: Order/PlaceOrder - Create order from checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(
    string shippingFullName,
    string shippingAddressLine1,
    string shippingAddressLine2,
    string shippingCity,
    string shippingPostalCode,
    string shippingCountry,
    string paymentMethod,
    bool isGift = false,
    string giftMessage = null)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Get cart items
                var cartItemsList = db.ShoppingCarts
                    .Where(c => c.UserID == userId)
                    .Include(c => c.ProductVariant.Product)
                    .Include(c => c.ProductVariant.Color)
                    .Include(c => c.ProductVariant.Size)
                    .ToList();

                if (!cartItemsList.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống";
                    return RedirectToAction("Checkout");
                }

                // Get Pending status
                var pendingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Pending");
                if (pendingStatus == null)
                {
                    InitializeOrderStatus();
                    pendingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Pending");
                }

                // Calculate totals
                decimal subTotal = cartItemsList.Sum(c => c.Quantity * c.ProductVariant.Price);
                decimal shippingAmount = 30000m;
                decimal taxAmount = 0m;
                decimal discountAmount = 0m;
                decimal totalAmount = subTotal + shippingAmount + taxAmount - discountAmount;

                // Generate order number
                string orderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Create new order object
                var newOrder = new Order();
                newOrder.OrderNumber = orderNumber;
                newOrder.UserID = userId;
                newOrder.OrderDate = DateTime.Now;
                newOrder.SubTotal = subTotal;
                newOrder.ShippingAmount = shippingAmount;
                newOrder.TaxAmount = taxAmount;
                newOrder.DiscountAmount = discountAmount;
                newOrder.TotalAmount = totalAmount;
                newOrder.OrderStatusID = pendingStatus?.StatusID;
                newOrder.ShippingFullName = shippingFullName;
                newOrder.ShippingAddressLine1 = shippingAddressLine1;
                newOrder.ShippingAddressLine2 = shippingAddressLine2;
                newOrder.ShippingCity = shippingCity;
                newOrder.ShippingPostalCode = shippingPostalCode;
                newOrder.ShippingCountry = shippingCountry;
                newOrder.PaymentMethod = paymentMethod;
                newOrder.PaymentStatus = "Unpaid";
                newOrder.IsGift = isGift;
                newOrder.GiftMessage = giftMessage;

                db.Orders.Add(newOrder);
                db.SaveChanges();

                // Create order items
                foreach (var cartItem in cartItemsList)
                {
                    var newOrderItem = new OrderItem();
                    newOrderItem.OrderID = newOrder.OrderID;
                    newOrderItem.VariantID = cartItem.VariantID;
                    newOrderItem.ProductName = cartItem.ProductVariant.Product.ProductName;
                    newOrderItem.SKU = cartItem.ProductVariant.SKU;
                    newOrderItem.ColorName = cartItem.ProductVariant.Color.ColorName;
                    newOrderItem.SizeName = cartItem.ProductVariant.Size.SizeName;
                    newOrderItem.Quantity = cartItem.Quantity;
                    newOrderItem.UnitPrice = cartItem.ProductVariant.Price;
                    newOrderItem.TotalPrice = cartItem.Quantity * cartItem.ProductVariant.Price;

                    db.OrderItems.Add(newOrderItem);

                    // Update inventory
                    var inventoryRecord = db.Inventories.FirstOrDefault(i => i.VariantID == cartItem.VariantID);
                    if (inventoryRecord != null)
                    {
                        inventoryRecord.QuantityReserved += cartItem.Quantity;
                        inventoryRecord.UpdatedAt = DateTime.Now;
                    }
                }

                // Clear cart
                db.ShoppingCarts.RemoveRange(cartItemsList);

                // Update session
                Session["CartCount"] = 0;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderConfirmation", new { id = newOrder.OrderID });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PlaceOrder Error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt hàng";
                return RedirectToAction("Checkout");
            }
        }


        // GET: Order/OrderConfirmation - Order success page
        public ActionResult OrderConfirmation(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                int userId = (int)Session["UserID"];

                var order = db.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant))
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Product))
                    .Include(o => o.OrderStatus)
                    .Include(o => o.User)
                    .FirstOrDefault(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("MyOrders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin đơn hàng";
                return RedirectToAction("MyOrders");
            }
        }

        // GET: Order/MyOrders
        public ActionResult MyOrders(int page = 1)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User", new { ReturnUrl = Url.Action("MyOrders", "Order") });
            }

            try
            {
                int userId = (int)Session["UserID"];

                if (!db.OrderStatus.Any())
                {
                    InitializeOrderStatus();
                }

                var pendingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Pending");

                var ordersWithoutStatus = db.Orders.Where(o => o.OrderStatusID == null).ToList();
                if (ordersWithoutStatus.Any() && pendingStatus != null)
                {
                    foreach (var orderToUpdate in ordersWithoutStatus)
                    {
                        orderToUpdate.OrderStatusID = pendingStatus.StatusID;
                    }
                    db.SaveChanges();
                }

                int pageSize = 10;

                var ordersQuery = db.Orders
                    .Where(o => o.UserID == userId)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate);

                int totalOrders = ordersQuery.Count();

                var pagedOrders = ordersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
                ViewBag.TotalOrders = totalOrders;

                return View(pagedOrders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MyOrders Error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Inner Exception: " + (ex.InnerException?.Message ?? "None"));

                TempData["ErrorMessage"] = "Không thể tải danh sách đơn hàng.";
                return View(new System.Collections.Generic.List<Order>());
            }
        }


        // GET: Order/OrderDetails - View specific order details
        public ActionResult OrderDetails(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                int userId = (int)Session["UserID"];

                var order = db.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant))
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Product))
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Color))
                    .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Size))
                    .Include(o => o.OrderStatus)
                    .FirstOrDefault(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("MyOrders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể tải chi tiết đơn hàng";
                return RedirectToAction("MyOrders");
            }
        }

        // POST: /Order/ConfirmReceived - Khách hàng xác nhận đã nhận hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReceived(int orderId)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                int userId = (int)Session["UserID"];

                // Tìm đơn hàng
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderID == orderId && o.UserID == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng của bạn" });
                }

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Order found: {order.OrderNumber}, Status: {order.OrderStatus?.StatusName}");

                // Kiểm tra trạng thái hiện tại
                if (order.OrderStatus == null || order.OrderStatus.StatusName != "Shipped")
                {
                    string currentStatus = order.OrderStatus?.StatusName ?? "null";
                    return Json(new { success = false, message = $"Chỉ có thể xác nhận đơn hàng đang giao. Trạng thái hiện tại: {currentStatus}" });
                }


                // Tìm status Delivered
                var deliveredStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Delivered");

                if (deliveredStatus == null)
                {
                    // Nếu chưa có, tự động tạo
                    deliveredStatus = new OrderStatus
                    {
                        StatusName = "Delivered",
                        Description = "Order delivered to customer",
                        SortOrder = 4
                    };
                    db.OrderStatus.Add(deliveredStatus);
                    db.SaveChanges();

                    System.Diagnostics.Debug.WriteLine("Created new Delivered status");
                }

                // Cập nhật trạng thái đơn hàng
                order.OrderStatusID = deliveredStatus.StatusID;
                order.UpdatedAt = DateTime.Now;
                order.DeliveredAt = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Updating order to Delivered status (StatusID: {deliveredStatus.StatusID})");

                // Cập nhật inventory
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    foreach (var item in order.OrderItems)
                    {
                        var variant = db.ProductVariants.Find(item.VariantID);
                        if (variant != null)
                        {
                            // Kiểm tra trước khi trừ
                            if (variant.QuantityReserved >= item.Quantity)
                            {
                                variant.QuantityReserved -= item.Quantity;
                                variant.QuantitySold += item.Quantity;

                                System.Diagnostics.Debug.WriteLine($"Updated variant {variant.SKU}: Reserved-{item.Quantity}, Sold+{item.Quantity}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: QuantityReserved ({variant.QuantityReserved}) < OrderQuantity ({item.Quantity}) for {variant.SKU}");
                            }
                        }
                    }
                }

                // Cập nhật payment status nếu COD
                if (order.PaymentMethod == "COD" && order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Paid";
                }

                // Lưu thay đổi
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Order confirmed received successfully");

                return Json(new
                {
                    success = true,
                    message = "Cảm ơn bạn đã xác nhận nhận hàng! Đơn hàng đã hoàn tất."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== ConfirmReceived ERROR ===");
                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }



        // POST: Order/CancelOrder - Khách hàng hủy đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int orderId)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                int userId = (int)Session["UserID"];

                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderID == orderId && o.UserID == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // ✅ KIỂM TRA NULL
                if (order.OrderStatus == null || order.OrderStatus.StatusName != "Pending")
                {
                    string currentStatus = order.OrderStatus?.StatusName ?? "Không xác định";
                    return Json(new { success = false, message = $"Chỉ có thể hủy đơn hàng đang chờ xử lý. Trạng thái hiện tại: {currentStatus}" });
                }

                // Tìm status "Cancelled"
                var cancelledStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Cancelled");
                if (cancelledStatus == null)
                {
                    cancelledStatus = new OrderStatus
                    {
                        StatusName = "Cancelled",
                        Description = "Order has been cancelled",
                        SortOrder = 5
                    };
                    db.OrderStatus.Add(cancelledStatus);
                    db.SaveChanges();
                }

                // Cập nhật trạng thái đơn hàng
                order.OrderStatusID = cancelledStatus.StatusID;
                order.UpdatedAt = DateTime.Now;

                // ✅ FIX: TRẢ LẠI INVENTORY
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    foreach (var item in order.OrderItems)
                    {
                        var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                        if (inventory != null)
                        {
                            if (inventory.QuantityReserved >= item.Quantity)
                            {
                                inventory.QuantityReserved -= item.Quantity;
                                inventory.UpdatedAt = DateTime.Now;
                                System.Diagnostics.Debug.WriteLine($"Returned {item.Quantity} to inventory for VariantID {item.VariantID}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: QuantityReserved ({inventory.QuantityReserved}) < OrderQuantity ({item.Quantity})");
                                inventory.QuantityReserved = Math.Max(0, inventory.QuantityReserved - item.Quantity);
                                inventory.UpdatedAt = DateTime.Now;
                            }
                        }
                    } // ✅ ĐÓNG FOREACH ĐÚNG CHỖ!
                }

                // ✅ SaveChanges PHẢI Ở NGOÀI foreach
                db.SaveChanges();

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CancelOrder Error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: /Order/RequestCancelOrder - Khách hàng yêu cầu hủy đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestCancelOrder(int orderId, string reason)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                int userId = (int)Session["UserID"];
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .FirstOrDefault(o => o.OrderID == orderId && o.UserID == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ yêu cầu hủy được đơn Pending
                if (order.OrderStatus == null || order.OrderStatus.StatusName != "Pending")
                {
                    string currentStatus = order.OrderStatus?.StatusName ?? "Không xác định";
                    return Json(new { success = false, message = $"Chỉ có thể yêu cầu hủy đơn hàng đang chờ xử lý. Trạng thái hiện tại: {currentStatus}" });
                }



                // Tìm status Pending Cancellation
                var pendingCancelStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Pending Cancellation");
                if (pendingCancelStatus == null)
                {
                    pendingCancelStatus = new OrderStatus
                    {
                        StatusName = "Pending Cancellation",
                        Description = "Customer requested to cancel order",
                        SortOrder = 6
                    };
                    db.OrderStatus.Add(pendingCancelStatus);
                    db.SaveChanges();
                }

                // Cập nhật trạng thái
                order.OrderStatusID = pendingCancelStatus.StatusID;
                order.CancellationReason = reason;
                order.CancellationRequestedAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true, message = "Đã gửi yêu cầu hủy đơn hàng. Vui lòng chờ admin xác nhận." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RequestCancelOrder Error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
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
