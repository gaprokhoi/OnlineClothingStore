using System;
using System.Collections.Generic;

namespace ClothingStoreWebApp.ViewModels
{
    public class InventoryViewModel
    {
        public int InventoryID { get; set; }
        public int VariantID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public string VariantSKU { get; set; }
        public string VariantColor { get; set; }
        public string VariantSize { get; set; }
        public decimal VariantPrice { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int AvailableQuantity { get; set; }
        public string StockStatus { get; set; }
        public string ImageURL { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryDashboardViewModel
    {
        public int TotalVariants { get; set; }
        public int TotalQuantityOnHand { get; set; }
        public int TotalQuantityReserved { get; set; }
        public int TotalAvailable { get; set; }
        public int LowStockVariants { get; set; }
        public int OutOfStockVariants { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<CategoryInventoryStats> CategoryStats { get; set; }
        public List<BrandInventoryStats> BrandStats { get; set; }
    }

    public class CategoryInventoryStats
    {
        public string CategoryName { get; set; }
        public int TotalQuantity { get; set; }
        public int VariantCount { get; set; }
        public int ProductCount { get; set; }
    }

    public class BrandInventoryStats
    {
        public string BrandName { get; set; }
        public int TotalQuantity { get; set; }
        public int VariantCount { get; set; }
        public int ProductCount { get; set; }
    }
}
