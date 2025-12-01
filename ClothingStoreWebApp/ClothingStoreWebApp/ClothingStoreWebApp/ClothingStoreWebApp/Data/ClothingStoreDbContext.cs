// Data/ClothingStoreDbContext.cs - Version đơn giản
using System.Data.Entity;
using ClothingStoreWebApp.Models;

namespace ClothingStoreWebApp.Data
{
    public class ClothingStoreDbContext : DbContext
    {
        public ClothingStoreDbContext() : base("DefaultConnection")
        {
            Database.SetInitializer<ClothingStoreDbContext>(null);
        }

        // DbSets for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<SeasonalCollection> SeasonalCollections { get; set; }  
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<OrderDiscount> OrderDiscounts { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match your database
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<UserRoleAssignment>().ToTable("UserRoleAssignments");
            modelBuilder.Entity<UserAddress>().ToTable("UserAddresses");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Brand>().ToTable("Brands");
            modelBuilder.Entity<SeasonalCollection>().ToTable("SeasonalCollections");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<ProductVariant>().ToTable("ProductVariants");
            modelBuilder.Entity<ProductImage>().ToTable("ProductImages");
            modelBuilder.Entity<Color>().ToTable("Colors");
            modelBuilder.Entity<Size>().ToTable("Sizes");
            modelBuilder.Entity<Inventory>().ToTable("Inventory");
            modelBuilder.Entity<ShoppingCart>().ToTable("ShoppingCart");
            modelBuilder.Entity<Wishlist>().ToTable("Wishlist");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
            modelBuilder.Entity<OrderStatus>().ToTable("OrderStatus");
            modelBuilder.Entity<OrderDiscount>().ToTable("OrderDiscounts");
            modelBuilder.Entity<DiscountCode>().ToTable("DiscountCodes");
            modelBuilder.Entity<ProductReview>().ToTable("ProductReviews");

            // Configure composite primary keys
            modelBuilder.Entity<UserRoleAssignment>()
                .HasKey(ura => new { ura.UserID, ura.RoleID });

            modelBuilder.Entity<OrderDiscount>()
                .HasKey(od => new { od.OrderID, od.DiscountID });

            // Configure relationship between ProductVariant and Inventory (one-to-many)
            modelBuilder.Entity<Inventory>()
                .HasRequired(i => i.ProductVariant)
                .WithMany()
                .HasForeignKey(i => i.VariantID);

            // Configure other important relationships
            modelBuilder.Entity<Category>()
                .HasOptional(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryID);
        }
    }
}