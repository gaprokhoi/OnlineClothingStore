using System;

namespace ClothingStoreWebApp.Models
{
    public class TopSellingProductViewModel
    {
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LowStockProductViewModel
    {
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string SKU { get; set; }
        public int Available { get; set; }
        public int Reserved { get; set; }
    }

    public class RecentOrderViewModel
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string StatusName { get; set; }
        public string CustomerName { get; set; }
    }
}
