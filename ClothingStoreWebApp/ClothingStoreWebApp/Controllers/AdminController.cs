using ClothingStoreWebApp.Data;
using ClothingStoreWebApp.Helpers;
using ClothingStoreWebApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OfficeOpenXml;


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
        // GET: /Admin (Dashboard với product table)
        public ActionResult Index(int? parentCategoryId, int? categoryId, int? brandId, string search, int page = 1)
        {
            var products = db.Products
                .Include(p => p.Category)
                .Include(p => p.Category.ParentCategory)
                .Include(p => p.Brand)
                .Include(p => p.Collection)
                .Include(p => p.ProductImages);

            // Apply filters
            if (parentCategoryId.HasValue && !categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryID == parentCategoryId.Value ||
                    p.Category.ParentCategoryID == parentCategoryId.Value);
            }
            else if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                products = products.Where(p => p.BrandID == brandId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.ProductName.Contains(search) || p.Description.Contains(search));
            }

            // Pagination
            int pageSize = 15;
            int totalProducts = products.Count();
            var pagedProducts = products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ===== DASHBOARD STATISTICS - DỮ LIỆU THẬT =====

            // 1. Product Statistics
            ViewBag.TotalProducts = db.Products.Count(p => p.IsActive);
            ViewBag.TotalCategories = db.Categories.Count(c => c.IsActive);
            ViewBag.TotalBrands = db.Brands.Count(b => b.IsActive);
            ViewBag.InactiveProducts = db.Products.Count(p => !p.IsActive);

            // 2. Inventory Statistics
            ViewBag.LowStockCount = db.Inventories
                .Count(i => (i.QuantityOnHand - i.QuantityReserved) < 20 && (i.QuantityOnHand - i.QuantityReserved) > 0);
            ViewBag.OutOfStockCount = db.Inventories
                .Count(i => (i.QuantityOnHand - i.QuantityReserved) <= 0);
            ViewBag.TotalInventoryValue = db.Inventories
                .Include(i => i.ProductVariant)
                .ToList()
                .Sum(i => i.QuantityOnHand * i.ProductVariant.Price);

            // 3. Order Statistics
            ViewBag.TotalOrders = db.Orders.Count();
            ViewBag.PendingOrdersCount = db.Orders.Count(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Pending");
            ViewBag.ProcessingOrdersCount = db.Orders.Count(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Processing");
            ViewBag.ShippedOrdersCount = db.Orders.Count(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Shipped");
            ViewBag.DeliveredOrdersCount = db.Orders.Count(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Delivered");
            ViewBag.CancelledOrdersCount = db.Orders.Count(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Cancelled");

            // 4. Revenue Statistics
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            // Doanh thu từ đơn hàng đã giao (Delivered)
            var deliveredOrders = db.Orders.Where(o =>
                o.OrderStatus != null && o.OrderStatus.StatusName == "Delivered");

            ViewBag.TodayRevenue = deliveredOrders
                .Where(o => DbFunctions.TruncateTime(o.OrderDate) == today)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.MonthRevenue = deliveredOrders
                .Where(o => o.OrderDate >= thisMonth)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.YearRevenue = deliveredOrders
                .Where(o => o.OrderDate >= thisYear)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalRevenue = deliveredOrders
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            // 5. Customer Statistics
            var customerRole = db.UserRoles.FirstOrDefault(r => r.RoleName == "Customer");
            if (customerRole != null)
            {
                var customerUserIds = db.UserRoleAssignments
                    .Where(ura => ura.RoleID == customerRole.RoleID)
                    .Select(ura => ura.UserID)
                    .ToList();

                ViewBag.TotalCustomers = db.Users
                    .Count(u => customerUserIds.Contains(u.UserID));

                ViewBag.NewCustomersThisMonth = db.Users
                    .Count(u => customerUserIds.Contains(u.UserID) && u.CreatedAt >= thisMonth);
            }
            else
            {
                ViewBag.TotalCustomers = 0;
                ViewBag.NewCustomersThisMonth = 0;
            }


            // 6. Top Selling Products (Top 5)
            var topProducts = db.OrderItems
                .Where(oi => oi.Order.OrderStatus != null &&
                             oi.Order.OrderStatus.StatusName == "Delivered")
                .GroupBy(oi => oi.ProductName)
                .Select(g => new TopSellingProductViewModel
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();
            ViewBag.TopSellingProducts = topProducts;

            // 7. Recent Orders (Latest 5)
            var recentOrders = db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList()
                .Select(o => new RecentOrderViewModel
                {
                    OrderID = o.OrderID,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    StatusName = o.OrderStatus.StatusName,
                    CustomerName = o.User != null ?
                        (o.User.FirstName + " " + o.User.LastName) :
                        o.ShippingFullName
                })
                .ToList();
            ViewBag.RecentOrders = recentOrders;

            // 8. Low Stock Products (Top 10 lowest)
            var lowStockProducts = db.Inventories
                .Include(i => i.ProductVariant)
                .Include(i => i.ProductVariant.Product)
                .Include(i => i.ProductVariant.Color)
                .Include(i => i.ProductVariant.Size)
                .ToList()
                .Where(i => (i.QuantityOnHand - i.QuantityReserved) < 20)
                .OrderBy(i => i.QuantityOnHand - i.QuantityReserved)
                .Take(10)
                .Select(i => new LowStockProductViewModel
                {
                    ProductName = i.ProductVariant.Product.ProductName,
                    Color = i.ProductVariant.Color?.ColorName,
                    Size = i.ProductVariant.Size?.SizeName,
                    SKU = i.ProductVariant.SKU,
                    Available = i.QuantityOnHand - i.QuantityReserved,
                    Reserved = i.QuantityReserved
                })
                .ToList();
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.Categories = db.Categories.Include(c => c.ParentCategory).ToList();
            ViewBag.Brands = db.Brands.OrderBy(b => b.BrandName).ToList();
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentParentCategoryId = parentCategoryId;
            ViewBag.CurrentBrandId = brandId;
            ViewBag.CurrentSearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            ViewBag.TotalProductsFound = totalProducts;

            return View(pagedProducts);
        }


        // GET: /Admin/ProductList - Quản lý sản phẩm với CRUD
        public ActionResult ProductList(int? parentCategoryId, int? categoryId, int? brandId, string search, int page = 1)
        {
            try
            {
                var products = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Category.ParentCategory)
                    .Include(p => p.Brand)
                    .Include(p => p.Collection)
                    .Include(p => p.ProductImages)
                    .AsQueryable();

                // Apply filters
                if (parentCategoryId.HasValue)
                {
                    products = products.Where(p => p.Category.ParentCategoryID == parentCategoryId.Value);
                }

                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.CategoryID == categoryId.Value);
                }

                if (brandId.HasValue)
                {
                    products = products.Where(p => p.BrandID == brandId.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    products = products.Where(p => p.ProductName.Contains(search))  ;
                }

                // Pagination
                int pageSize = 10;
                int totalProducts = products.Count();
                var productList = products.OrderByDescending(p => p.CreatedAt)
                                           .Skip((page - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToList();

                // Pass filter data
                ViewBag.ParentCategories = db.Categories.Where(c => c.ParentCategoryID == null).ToList();
                ViewBag.Categories = db.Categories.Where(c => c.ParentCategoryID != null).ToList();
                ViewBag.Brands = db.Brands.ToList();
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.TotalProducts = totalProducts;

                return View(productList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách sản phẩm: " + ex.Message;
                return RedirectToAction("Index");
            }
        }



        // AJAX: Get sub categories by parent ID (giống ProductController)
        [HttpGet]
        public JsonResult GetSubCategories(int parentId)
        {
            try
            {
                var subCategories = db.Categories
                    .Where(c => c.ParentCategoryID == parentId)
                    .Select(c => new
                    {
                        CategoryID = c.CategoryID,
                        CategoryName = c.CategoryName,
                        ParentCategoryID = c.ParentCategoryID
                    })
                    .ToList();

                return Json(subCategories, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // GET: /Admin/ManageOrders - Quản lý đơn hàng
        public ActionResult ManageOrders(string status = "All", string search = "", int page = 1)
        {
            try
            {
                // Query cơ bản - KHÔNG filter OrderStatus ngay đây
                var query = db.Orders
                    .Include(o => o.OrderStatus) 
                    .OrderByDescending(o => o.OrderDate)
                    .AsQueryable();

                // Filter by status
                if (status != "All")
                {
                    query = query.Where(o => o.OrderStatus != null && o.OrderStatus.StatusName == status);
                }

                // Search by order number or customer name
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(search) ||
                        o.ShippingFullName.Contains(search)
                    );
                }

                // Pagination
                int pageSize = 10;
                int totalOrders = query.Count();

                // Include AFTER filtering
                var orders = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ToList();

                // Pass data to view
                ViewBag.CurrentStatus = status;
                ViewBag.SearchTerm = search;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

                // Statistics - SỬA ĐÂY: Thêm null check
                ViewBag.PendingCount = db.Orders.Count(o => o.OrderStatus != null && o.OrderStatus.StatusName == "Pending");
                ViewBag.ProcessingCount = db.Orders.Count(o => o.OrderStatus != null && o.OrderStatus.StatusName == "Processing");
                ViewBag.ShippedCount = db.Orders.Count(o => o.OrderStatus != null && o.OrderStatus.StatusName == "Shipped");
                ViewBag.DeliveredCount = db.Orders.Count(o => o.OrderStatus != null && o.OrderStatus.StatusName == "Delivered");

                return View(orders);
            }
            catch (Exception ex)
            {
                // Log chi tiết
                System.Diagnostics.Debug.WriteLine("===== ManageOrders ERROR =====");
                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                    System.Diagnostics.Debug.WriteLine("Inner Stack Trace: " + ex.InnerException.StackTrace);
                }

                TempData["ErrorMessage"] = "Lỗi khi tải danh sách đơn hàng: " + ex.Message +
                    (ex.InnerException != null ? " - " + ex.InnerException.Message : "");
                return RedirectToAction("Index");
            }
        }


        // POST: /Admin/ApproveOrder - Duyệt đơn hàng (Pending -> Processing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveOrder(int orderId)
        {
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null || order.OrderStatus.StatusName != "Pending")
                {
                    return Json(new { success = false, message = "Không thể duyệt đơn hàng" });
                }

                // ✅ KIỂM TRA STATUS TRƯỚC - FAIL FAST
                var processingStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Processing");
                if (processingStatus == null)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống: Không tìm thấy status Processing" });
                }

                // ✅ SAU ĐÓ MỚI TRỪ KHO
                foreach (var item in order.OrderItems)
                {
                    var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                    if (inventory == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Không tìm thấy inventory cho {item.ProductName}"
                        });
                    }

                    if (inventory.QuantityOnHand < item.Quantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"{item.ProductName} không đủ hàng"
                        });
                    }

                    // Trừ kho
                    int qtyBefore = inventory.QuantityOnHand;
                    inventory.QuantityReserved -= item.Quantity;
                    inventory.QuantityOnHand -= item.Quantity;
                    inventory.UpdatedAt = DateTime.Now;

                    //Ghi log(nếu có InventoryTransaction model hoàn chỉnh)
                    var transaction = new InventoryTransaction
                    {
                        VariantID = item.VariantID,
                        TransactionType = "OUT",
                        Quantity = -item.Quantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = inventory.QuantityOnHand,
                        Reason = $"Approved Order #{order.OrderNumber}",
                        OrderID = order.OrderID,
                        UserID = (int?)Session["UserID"],
                        CreatedAt = DateTime.Now
                    };
                    db.InventoryTransactions.Add(transaction);
                }

                // ✅ CẬP NHẬT STATUS - KHÔNG CẦN IF NỮA (đã check ở trên)
                order.OrderStatusID = processingStatus.StatusID;
                order.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true, message = "Đã duyệt và trừ kho thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }


        // POST: /Admin/ShipOrder - Đánh dấu đơn hàng đã giao cho shipper (Processing -> Shipped)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ShipOrder(int orderId)
        {
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ ship đơn đang ở trạng thái Processing
                if (order.OrderStatus.StatusName != "Processing")
                {
                    return Json(new { success = false, message = "Đơn hàng chưa được duyệt hoặc đã giao" });
                }

                // Cập nhật sang Shipped
                var shippedStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Shipped");
                if (shippedStatus != null)
                {
                    order.OrderStatusID = shippedStatus.StatusID;
                    order.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã cập nhật đơn hàng sang trạng thái đang giao" });
                }

                return Json(new { success = false, message = "Không tìm thấy trạng thái Shipped" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: /Admin/ConfirmDelivered - Admin xác nhận đã giao hàng thành công (Shipped -> Delivered)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDelivered(int orderId)
        {
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ confirm đơn đang ở trạng thái Shipped
                if (order.OrderStatus.StatusName != "Shipped")
                {
                    return Json(new { success = false, message = "Đơn hàng chưa được giao" });
                }

                // Cập nhật sang Delivered
                var deliveredStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Delivered");
                if (deliveredStatus != null)
                {
                    order.OrderStatusID = deliveredStatus.StatusID;
                    order.UpdatedAt = DateTime.Now;

                    // Cập nhật inventory: trừ QuantityReserved, cộng QuantitySold
                    foreach (var item in order.OrderItems)
                    {
                        var variant = db.ProductVariants.Find(item.VariantID);
                        if (variant != null)
                        {
                            variant.QuantitySold += item.Quantity;
                        }
                    }

                    // Cập nhật payment status nếu COD
                    if (order.PaymentMethod == "COD" && order.PaymentStatus != "Paid")
                    {
                        order.PaymentStatus = "Paid";
                    }

                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã xác nhận giao hàng thành công" });
                }

                return Json(new { success = false, message = "Không tìm thấy trạng thái Delivered" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        // POST: /Order/CancelOrder - Khách hàng hủy đơn hàng
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

                // Chỉ hủy được đơn Pending
                if (order.OrderStatus.StatusName != "Pending")
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xử lý" });
                }

                // Tìm status Cancelled
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

                // ✅ HOÀN TRẢ KHO
                foreach (var item in order.OrderItems)
                {
                    // Lấy từ ProductVariant (cách cũ - nếu có)
                    var variant = db.ProductVariants.Find(item.VariantID);
                    if (variant != null)
                    {
                        variant.QuantityReserved -= item.Quantity;
                    }

                    // ✅ HOÀN TRẢ INVENTORY (THÊM MỚI)
                    var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                    if (inventory != null)
                    {
                        inventory.QuantityOnHand += item.Quantity; // ← CỘNG LẠI
                        inventory.UpdatedAt = DateTime.Now;

                        System.Diagnostics.Debug.WriteLine(
                            $"[CANCEL] Restored {item.Quantity} to VariantID {item.VariantID}. New OnHand: {inventory.QuantityOnHand}"
                        );
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Đã hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CancelOrder Error: " + ex.Message);
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/ApproveCancellation - Admin chấp nhận yêu cầu hủy
        // POST: Admin/ApproveCancellation - Admin chấp nhận yêu cầu hủy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveCancellation(int orderId)
        {
            try
            {
                var order = db.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ chấp nhận hủy đơn đang trạng thái "Pending Cancellation"
                if (order.OrderStatus.StatusName != "Pending Cancellation")
                {
                    return Json(new { success = false, message = "Đơn hàng không có yêu cầu hủy" });
                }

                // Tìm status Cancelled
                var cancelledStatus = db.OrderStatus.FirstOrDefault(s => s.StatusName == "Cancelled");
                if (cancelledStatus != null)
                {
                    // Cập nhật trạng thái sang Cancelled
                    order.OrderStatusID = cancelledStatus.StatusID;
                    order.UpdatedAt = DateTime.Now;

                    // ✅ HOÀN TRẢ KHO
                    foreach (var item in order.OrderItems)
                    {
                        // Trả lại QuantityReserved từ ProductVariant
                        var variant = db.ProductVariants.Find(item.VariantID);
                        if (variant != null)
                        {
                            variant.QuantityReserved -= item.Quantity;
                        }

                        // ✅ HOÀN TRẢ INVENTORY (THÊM MỚI)
                        var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                        if (inventory != null)
                        {
                            inventory.QuantityOnHand += item.Quantity; // ← CỘNG LẠI
                            inventory.UpdatedAt = DateTime.Now;

                            System.Diagnostics.Debug.WriteLine(
                                $"[APPROVE CANCEL] Restored {item.Quantity} to VariantID {item.VariantID}. New OnHand: {inventory.QuantityOnHand}"
                            );
                        }
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã chấp nhận hủy đơn hàng" });
                }

                return Json(new { success = false, message = "Không tìm thấy trạng thái Cancelled" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }



        // GET: /Admin/OrderDetails/5
        public ActionResult OrderDetails(int id)
        {
            var order = db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.OrderItems.Select(oi => oi.ProductVariant))
                .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Product))
                .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Color))
                .Include(o => o.OrderItems.Select(oi => oi.ProductVariant.Size))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("ManageOrders");
            }

            // Get all order statuses for dropdown
            ViewBag.OrderStatuses = db.OrderStatus.OrderBy(os => os.StatusID).ToList();

            return View(order);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, int statusId)
        {
            var order = db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            // Lưu trạng thái cũ
            var oldStatusName = order.OrderStatus?.StatusName;

            // Cập nhật trạng thái mới
            order.OrderStatusID = statusId;  // ✅ ĐÚNG
            order.UpdatedAt = DateTime.Now;

            // Lấy thông tin trạng thái mới - DÙNG OrderStatus (số ít)
            var newStatus = db.OrderStatus.Find(statusId);  // ✅ OrderStatus (số ít)

            if (newStatus != null)
            {
                // Nếu chuyển sang "Shipped"
                if (newStatus.StatusName == "Shipped" && oldStatusName != "Shipped")
                {
                    order.ShippedAt = DateTime.Now;

                    foreach (var item in order.OrderItems)
                    {
                        var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                        if (inventory != null)
                        {
                            inventory.QuantityReserved -= item.Quantity;
                            inventory.QuantityOnHand -= item.Quantity;
                        }
                    }
                }

                // Nếu chuyển sang "Delivered"
                if (newStatus.StatusName == "Delivered" && oldStatusName != "Delivered")
                {
                    order.DeliveredAt = DateTime.Now;
                    order.PaymentStatus = "Paid";
                }

                // Nếu chuyển sang "Cancelled"
                if (newStatus.StatusName == "Cancelled" && oldStatusName != "Cancelled")
                {
                    foreach (var item in order.OrderItems)
                    {
                        var inventory = db.Inventories.FirstOrDefault(i => i.VariantID == item.VariantID);
                        if (inventory != null)
                        {
                            inventory.QuantityReserved -= item.Quantity;

                            if (oldStatusName == "Shipped")
                            {
                                inventory.QuantityOnHand += item.Quantity;
                            }
                        }
                    }
                }
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return Json(new { success = true, message = "Cập nhật thành công!" });
        }


        // GET: Admin/ManageInventory
        public ActionResult ManageInventory(string search, bool? lowStockOnly, int page = 1)
        {
            try
            {
                int pageSize = 20;

                // Base query với all includes
                var query = db.Inventories
                    .Include(i => i.ProductVariant)
                    .Include(i => i.ProductVariant.Product)
                    .Include(i => i.ProductVariant.Color)
                    .Include(i => i.ProductVariant.Size)
                    .Include(i => i.ProductVariant.Product.Brand)
                    .AsQueryable();

                // Filter: Search by product name or SKU
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(i =>
                        i.ProductVariant.Product.ProductName.Contains(search) ||
                        i.ProductVariant.SKU.Contains(search));
                }

                // Filter: Low stock only (< 20 available)
                if (lowStockOnly == true)
                {
                    query = query.Where(i =>
                        (i.QuantityOnHand - i.QuantityReserved) < 20);
                }

                // Get total count for pagination
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Get paged results
                var inventories = query
                    .OrderBy(i => i.ProductVariant.Product.ProductName)
                    .ThenBy(i => i.ProductVariant.SKU)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass data to view
                ViewBag.Search = search;
                ViewBag.LowStockOnly = lowStockOnly;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                // Low stock count for badge
                ViewBag.LowStockCount = db.Inventories
                    .Count(i => (i.QuantityOnHand - i.QuantityReserved) < 20);

                return View(inventories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageInventory Error: {ex.Message}");
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách kho hàng.";
                return RedirectToAction("Index");
            }
        }


        [HttpGet]
        public ActionResult Export(int? parentCategoryId, int? categoryId, int? brandId, string search)
        {
            try
            {
                var products = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive)
                    .AsQueryable();

                // Apply filters
                if (parentCategoryId.HasValue && !categoryId.HasValue)
                {
                    products = products.Where(p => p.Category.ParentCategoryID == parentCategoryId);
                }
                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.CategoryID == categoryId);
                }
                if (brandId.HasValue)
                {
                    products = products.Where(p => p.BrandID == brandId);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    products = products.Where(p => p.ProductName.Contains(search) || p.Description.Contains(search));
                }

                var productList = products.OrderBy(p => p.ProductName).ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Danh sách sản phẩm");

                    // Header
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 3].Value = "Danh mục";
                    worksheet.Cells[1, 4].Value = "Thương hiệu";
                    worksheet.Cells[1, 5].Value = "Giá gốc";
                    worksheet.Cells[1, 6].Value = "Giới tính";
                    worksheet.Cells[1, 7].Value = "Mô tả";
                    worksheet.Cells[1, 8].Value = "Ngày tạo";

                    // Data
                    int row = 2;
                    foreach (var product in productList)
                    {
                        worksheet.Cells[row, 1].Value = product.ProductID;
                        worksheet.Cells[row, 2].Value = product.ProductName;
                        worksheet.Cells[row, 3].Value = product.Category?.CategoryName ?? "N/A";
                        worksheet.Cells[row, 4].Value = product.Brand?.BrandName ?? "N/A";
                        worksheet.Cells[row, 5].Value = product.BasePrice;
                        worksheet.Cells[row, 6].Value = product.Gender ?? "N/A";
                        worksheet.Cells[row, 7].Value = product.Description ?? "";
                        worksheet.Cells[row, 8].Value = product.CreatedAt.ToString("dd/MM/yyyy");
                        row++;
                    }

                    // Auto-fit
                    worksheet.Cells.AutoFitColumns();

                    var fileName = $"DanhSachSanPham_{DateTime.Now:ddMMyyyy_HHmmss}.xlsx";
                    var fileBytes = package.GetAsByteArray();

                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // ✅ Log error, không redirect
                System.Diagnostics.Debug.WriteLine("Export Error: " + ex.Message);
                return RedirectToAction("ProductList", new { errorMessage = ex.Message });
            }
        }

        // POST: Admin/UpdateStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateStock(int inventoryId, int newQuantity)
        {
            try
            {
                var inventory = db.Inventories.Find(inventoryId);

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy inventory." });
                }

                if (newQuantity < 0)
                {
                    return Json(new { success = false, message = "Số lượng không được âm." });
                }

                // Check if new quantity can cover reserved quantity
                if (newQuantity < inventory.QuantityReserved)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Số lượng tối thiểu phải là {inventory.QuantityReserved} (đã có {inventory.QuantityReserved} sản phẩm đang được đặt)."
                    });
                }

                // Update
                inventory.QuantityOnHand = newQuantity;
                inventory.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                int available = inventory.QuantityOnHand - inventory.QuantityReserved;

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thành công!",
                    newQuantity = inventory.QuantityOnHand,
                    available = available,
                    reserved = inventory.QuantityReserved
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStock Error: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi cập nhật số lượng." });
            }
        }

        // POST: Admin/AdjustStock (Add/Remove)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AdjustStock(int inventoryId, int adjustment, string reason)
        {
            try
            {
                var inventory = db.Inventories
                    .Include(i => i.ProductVariant)
                    .FirstOrDefault(i => i.InventoryID == inventoryId);

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy inventory." });
                }

                int newQuantity = inventory.QuantityOnHand + adjustment;

                if (newQuantity < 0)
                {
                    return Json(new { success = false, message = "Số lượng không đủ để trừ." });
                }

                if (newQuantity < inventory.QuantityReserved)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể giảm xuống dưới {inventory.QuantityReserved} (đã có sản phẩm đang được đặt)."
                    });
                }

                // Update inventory
                inventory.QuantityOnHand = newQuantity;
                inventory.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                int available = inventory.QuantityOnHand - inventory.QuantityReserved;

                return Json(new
                {
                    success = true,
                    message = $"Đã {(adjustment > 0 ? "thêm" : "trừ")} {Math.Abs(adjustment)} sản phẩm!",
                    newQuantity = inventory.QuantityOnHand,
                    available = available,
                    reserved = inventory.QuantityReserved
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdjustStock Error: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi điều chỉnh số lượng." });
            }
        }

        // GET: Admin/GetLowStockCount (AJAX)
        public JsonResult GetLowStockCount()
        {
            try
            {
                int count = db.Inventories
                    .Count(i => (i.QuantityOnHand - i.QuantityReserved) < 20);

                return Json(new { success = true, count = count }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false, count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}
