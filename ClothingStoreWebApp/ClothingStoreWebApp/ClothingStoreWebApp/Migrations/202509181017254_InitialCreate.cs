namespace ClothingStoreWebApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Brands",
                c => new
                    {
                        BrandID = c.Int(nullable: false, identity: true),
                        BrandName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(maxLength: 500),
                        LogoURL = c.String(maxLength: 255),
                        Website = c.String(maxLength: 255),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.BrandID);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        ProductID = c.Int(nullable: false, identity: true),
                        ProductName = c.String(nullable: false, maxLength: 255),
                        Description = c.String(),
                        CategoryID = c.Int(nullable: false),
                        BrandID = c.Int(nullable: false),
                        CollectionID = c.Int(),
                        Material = c.String(maxLength: 200),
                        CareInstructions = c.String(maxLength: 500),
                        Gender = c.String(nullable: false, maxLength: 20),
                        BasePrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                        IsFeatured = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ProductID)
                .ForeignKey("dbo.Brands", t => t.BrandID, cascadeDelete: true)
                .ForeignKey("dbo.Categories", t => t.CategoryID, cascadeDelete: true)
                .ForeignKey("dbo.SeasonalCollections", t => t.CollectionID)
                .Index(t => t.CategoryID)
                .Index(t => t.BrandID)
                .Index(t => t.CollectionID);
            
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        CategoryID = c.Int(nullable: false, identity: true),
                        CategoryName = c.String(nullable: false, maxLength: 100),
                        ParentCategoryID = c.Int(),
                        Description = c.String(maxLength: 500),
                        ImageURL = c.String(maxLength: 255),
                        SortOrder = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CategoryID)
                .ForeignKey("dbo.Categories", t => t.ParentCategoryID)
                .Index(t => t.ParentCategoryID);
            
            CreateTable(
                "dbo.SeasonalCollections",
                c => new
                    {
                        CollectionID = c.Int(nullable: false, identity: true),
                        CollectionName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(maxLength: 500),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        ImageURL = c.String(maxLength: 255),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CollectionID);
            
            CreateTable(
                "dbo.ProductImages",
                c => new
                    {
                        ImageID = c.Int(nullable: false, identity: true),
                        ProductID = c.Int(nullable: false),
                        ColorID = c.Int(),
                        ImageURL = c.String(nullable: false, maxLength: 500),
                        AltText = c.String(maxLength: 255),
                        SortOrder = c.Int(nullable: false),
                        ImageType = c.String(maxLength: 50),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ImageID)
                .ForeignKey("dbo.Colors", t => t.ColorID)
                .ForeignKey("dbo.Products", t => t.ProductID, cascadeDelete: true)
                .Index(t => t.ProductID)
                .Index(t => t.ColorID);
            
            CreateTable(
                "dbo.Colors",
                c => new
                    {
                        ColorID = c.Int(nullable: false, identity: true),
                        ColorName = c.String(nullable: false, maxLength: 50),
                        ColorCode = c.String(nullable: false, maxLength: 10),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ColorID);
            
            CreateTable(
                "dbo.ProductVariants",
                c => new
                    {
                        VariantID = c.Int(nullable: false, identity: true),
                        ProductID = c.Int(nullable: false),
                        SKU = c.String(nullable: false, maxLength: 100),
                        ColorID = c.Int(nullable: false),
                        SizeID = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.VariantID)
                .ForeignKey("dbo.Colors", t => t.ColorID, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.ProductID, cascadeDelete: true)
                .ForeignKey("dbo.Sizes", t => t.SizeID, cascadeDelete: true)
                .Index(t => t.ProductID)
                .Index(t => t.ColorID)
                .Index(t => t.SizeID);
            
            CreateTable(
                "dbo.OrderItems",
                c => new
                    {
                        OrderItemID = c.Int(nullable: false, identity: true),
                        OrderID = c.Int(nullable: false),
                        VariantID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProductName = c.String(nullable: false, maxLength: 255),
                        SKU = c.String(nullable: false, maxLength: 100),
                        ColorName = c.String(nullable: false, maxLength: 50),
                        SizeName = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.OrderItemID)
                .ForeignKey("dbo.Orders", t => t.OrderID, cascadeDelete: true)
                .ForeignKey("dbo.ProductVariants", t => t.VariantID, cascadeDelete: true)
                .Index(t => t.OrderID)
                .Index(t => t.VariantID);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        OrderID = c.Int(nullable: false, identity: true),
                        OrderNumber = c.String(nullable: false, maxLength: 50),
                        UserID = c.Int(nullable: false),
                        OrderStatusID = c.Int(nullable: false),
                        OrderDate = c.DateTime(nullable: false),
                        SubTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ShippingAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ShippingFullName = c.String(nullable: false, maxLength: 200),
                        ShippingAddressLine1 = c.String(nullable: false, maxLength: 255),
                        ShippingAddressLine2 = c.String(maxLength: 255),
                        ShippingCity = c.String(nullable: false, maxLength: 100),
                        ShippingPostalCode = c.String(nullable: false, maxLength: 20),
                        ShippingCountry = c.String(nullable: false, maxLength: 100),
                        PaymentMethod = c.String(maxLength: 50),
                        PaymentStatus = c.String(maxLength: 50),
                        TrackingNumber = c.String(maxLength: 100),
                        ShippedAt = c.DateTime(),
                        DeliveredAt = c.DateTime(),
                        IsGift = c.Boolean(nullable: false),
                        GiftMessage = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                        OrderStatus_StatusID = c.Int(),
                    })
                .PrimaryKey(t => t.OrderID)
                .ForeignKey("dbo.OrderStatus", t => t.OrderStatus_StatusID)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID)
                .Index(t => t.OrderStatus_StatusID);
            
            CreateTable(
                "dbo.OrderDiscounts",
                c => new
                    {
                        OrderID = c.Int(nullable: false),
                        DiscountID = c.Int(nullable: false),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.OrderID, t.DiscountID })
                .ForeignKey("dbo.DiscountCodes", t => t.DiscountID, cascadeDelete: true)
                .ForeignKey("dbo.Orders", t => t.OrderID, cascadeDelete: true)
                .Index(t => t.OrderID)
                .Index(t => t.DiscountID);
            
            CreateTable(
                "dbo.DiscountCodes",
                c => new
                    {
                        DiscountID = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 255),
                        DiscountType = c.String(nullable: false, maxLength: 20),
                        DiscountValue = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MinimumOrderAmount = c.Decimal(precision: 18, scale: 2),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.DiscountID);
            
            CreateTable(
                "dbo.OrderStatus",
                c => new
                    {
                        StatusID = c.Int(nullable: false, identity: true),
                        StatusName = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 255),
                        SortOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.StatusID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserID = c.Int(nullable: false, identity: true),
                        Email = c.String(nullable: false, maxLength: 255),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        FirstName = c.String(nullable: false, maxLength: 100),
                        LastName = c.String(nullable: false, maxLength: 100),
                        PhoneNumber = c.String(maxLength: 20),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UserID);
            
            CreateTable(
                "dbo.ProductReviews",
                c => new
                    {
                        ReviewID = c.Int(nullable: false, identity: true),
                        ProductID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                        ReviewTitle = c.String(maxLength: 255),
                        ReviewText = c.String(),
                        IsApproved = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ReviewID)
                .ForeignKey("dbo.Products", t => t.ProductID, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.ProductID)
                .Index(t => t.UserID);
            
            CreateTable(
                "dbo.ShoppingCart",
                c => new
                    {
                        CartID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        VariantID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        AddedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CartID)
                .ForeignKey("dbo.ProductVariants", t => t.VariantID, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID)
                .Index(t => t.VariantID);
            
            CreateTable(
                "dbo.UserAddresses",
                c => new
                    {
                        AddressID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        AddressType = c.String(nullable: false, maxLength: 20),
                        FullName = c.String(nullable: false, maxLength: 200),
                        AddressLine1 = c.String(nullable: false, maxLength: 255),
                        AddressLine2 = c.String(maxLength: 255),
                        City = c.String(nullable: false, maxLength: 100),
                        State = c.String(maxLength: 100),
                        PostalCode = c.String(nullable: false, maxLength: 20),
                        Country = c.String(nullable: false, maxLength: 100),
                        IsDefault = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AddressID)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID);
            
            CreateTable(
                "dbo.UserRoleAssignments",
                c => new
                    {
                        UserID = c.Int(nullable: false),
                        RoleID = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserID, t.RoleID })
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .ForeignKey("dbo.UserRoles", t => t.RoleID, cascadeDelete: true)
                .Index(t => t.UserID)
                .Index(t => t.RoleID);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        RoleID = c.Int(nullable: false, identity: true),
                        RoleName = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.RoleID);
            
            CreateTable(
                "dbo.Wishlist",
                c => new
                    {
                        WishlistID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        ProductID = c.Int(nullable: false),
                        AddedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.WishlistID)
                .ForeignKey("dbo.Products", t => t.ProductID, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID)
                .Index(t => t.ProductID);
            
            CreateTable(
                "dbo.Sizes",
                c => new
                    {
                        SizeID = c.Int(nullable: false, identity: true),
                        SizeName = c.String(nullable: false, maxLength: 20),
                        SizeOrder = c.Int(nullable: false),
                        Category = c.String(nullable: false, maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.SizeID);
            
            CreateTable(
                "dbo.Inventory",
                c => new
                    {
                        InventoryID = c.Int(nullable: false, identity: true),
                        VariantID = c.Int(nullable: false),
                        QuantityOnHand = c.Int(nullable: false),
                        QuantityReserved = c.Int(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.InventoryID)
                .ForeignKey("dbo.ProductVariants", t => t.VariantID, cascadeDelete: true)
                .Index(t => t.VariantID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Inventory", "VariantID", "dbo.ProductVariants");
            DropForeignKey("dbo.ProductImages", "ProductID", "dbo.Products");
            DropForeignKey("dbo.ProductVariants", "SizeID", "dbo.Sizes");
            DropForeignKey("dbo.ProductVariants", "ProductID", "dbo.Products");
            DropForeignKey("dbo.OrderItems", "VariantID", "dbo.ProductVariants");
            DropForeignKey("dbo.Wishlist", "UserID", "dbo.Users");
            DropForeignKey("dbo.Wishlist", "ProductID", "dbo.Products");
            DropForeignKey("dbo.UserRoleAssignments", "RoleID", "dbo.UserRoles");
            DropForeignKey("dbo.UserRoleAssignments", "UserID", "dbo.Users");
            DropForeignKey("dbo.UserAddresses", "UserID", "dbo.Users");
            DropForeignKey("dbo.ShoppingCart", "UserID", "dbo.Users");
            DropForeignKey("dbo.ShoppingCart", "VariantID", "dbo.ProductVariants");
            DropForeignKey("dbo.ProductReviews", "UserID", "dbo.Users");
            DropForeignKey("dbo.ProductReviews", "ProductID", "dbo.Products");
            DropForeignKey("dbo.Orders", "UserID", "dbo.Users");
            DropForeignKey("dbo.Orders", "OrderStatus_StatusID", "dbo.OrderStatus");
            DropForeignKey("dbo.OrderItems", "OrderID", "dbo.Orders");
            DropForeignKey("dbo.OrderDiscounts", "OrderID", "dbo.Orders");
            DropForeignKey("dbo.OrderDiscounts", "DiscountID", "dbo.DiscountCodes");
            DropForeignKey("dbo.ProductVariants", "ColorID", "dbo.Colors");
            DropForeignKey("dbo.ProductImages", "ColorID", "dbo.Colors");
            DropForeignKey("dbo.Products", "CollectionID", "dbo.SeasonalCollections");
            DropForeignKey("dbo.Products", "CategoryID", "dbo.Categories");
            DropForeignKey("dbo.Categories", "ParentCategoryID", "dbo.Categories");
            DropForeignKey("dbo.Products", "BrandID", "dbo.Brands");
            DropIndex("dbo.Inventory", new[] { "VariantID" });
            DropIndex("dbo.Wishlist", new[] { "ProductID" });
            DropIndex("dbo.Wishlist", new[] { "UserID" });
            DropIndex("dbo.UserRoleAssignments", new[] { "RoleID" });
            DropIndex("dbo.UserRoleAssignments", new[] { "UserID" });
            DropIndex("dbo.UserAddresses", new[] { "UserID" });
            DropIndex("dbo.ShoppingCart", new[] { "VariantID" });
            DropIndex("dbo.ShoppingCart", new[] { "UserID" });
            DropIndex("dbo.ProductReviews", new[] { "UserID" });
            DropIndex("dbo.ProductReviews", new[] { "ProductID" });
            DropIndex("dbo.OrderDiscounts", new[] { "DiscountID" });
            DropIndex("dbo.OrderDiscounts", new[] { "OrderID" });
            DropIndex("dbo.Orders", new[] { "OrderStatus_StatusID" });
            DropIndex("dbo.Orders", new[] { "UserID" });
            DropIndex("dbo.OrderItems", new[] { "VariantID" });
            DropIndex("dbo.OrderItems", new[] { "OrderID" });
            DropIndex("dbo.ProductVariants", new[] { "SizeID" });
            DropIndex("dbo.ProductVariants", new[] { "ColorID" });
            DropIndex("dbo.ProductVariants", new[] { "ProductID" });
            DropIndex("dbo.ProductImages", new[] { "ColorID" });
            DropIndex("dbo.ProductImages", new[] { "ProductID" });
            DropIndex("dbo.Categories", new[] { "ParentCategoryID" });
            DropIndex("dbo.Products", new[] { "CollectionID" });
            DropIndex("dbo.Products", new[] { "BrandID" });
            DropIndex("dbo.Products", new[] { "CategoryID" });
            DropTable("dbo.Inventory");
            DropTable("dbo.Sizes");
            DropTable("dbo.Wishlist");
            DropTable("dbo.UserRoles");
            DropTable("dbo.UserRoleAssignments");
            DropTable("dbo.UserAddresses");
            DropTable("dbo.ShoppingCart");
            DropTable("dbo.ProductReviews");
            DropTable("dbo.Users");
            DropTable("dbo.OrderStatus");
            DropTable("dbo.DiscountCodes");
            DropTable("dbo.OrderDiscounts");
            DropTable("dbo.Orders");
            DropTable("dbo.OrderItems");
            DropTable("dbo.ProductVariants");
            DropTable("dbo.Colors");
            DropTable("dbo.ProductImages");
            DropTable("dbo.SeasonalCollections");
            DropTable("dbo.Categories");
            DropTable("dbo.Products");
            DropTable("dbo.Brands");
        }
    }
}
