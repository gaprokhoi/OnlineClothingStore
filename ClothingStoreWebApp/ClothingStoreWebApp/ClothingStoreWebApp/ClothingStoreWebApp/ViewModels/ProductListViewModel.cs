using ClothingStoreWebApp.Models;
using System;
using System.Collections.Generic;

public class ProductListViewModel
{
    public IEnumerable<ProductItemViewModel> Products { get; set; }
    public int TotalProducts { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);

    // Filters
    public string SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? SeasonCollectionId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<int> SelectedColors { get; set; } = new List<int>();
    public string SortBy { get; set; } = "name"; // name, price_asc, price_desc, newest

    // Filter options
    public IEnumerable<Category> Categories { get; set; }
    public IEnumerable<Brand> Brands { get; set; }
    public IEnumerable<SeasonalCollection> SeasonCollections { get; set; }
    public IEnumerable<Color> Colors { get; set; }
    public decimal MinPriceRange { get; set; }
    public decimal MaxPriceRange { get; set; }
}

public class ProductItemViewModel
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public string ShortDescription { get; set; }
    public string CategoryName { get; set; }
    public string BrandName { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public string MainImageUrl { get; set; }
    public bool HasMultipleVariants { get; set; }
    public int TotalVariants { get; set; }
    public List<string> AvailableColors { get; set; } = new List<string>();
    public bool IsInStock { get; set; }
    public decimal? DiscountPercentage { get; set; }
}
