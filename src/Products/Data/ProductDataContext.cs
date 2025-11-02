using DataEntities;
using Microsoft.EntityFrameworkCore;

namespace Products.Data;

public class ProductDataContext : DbContext
{
    public ProductDataContext(DbContextOptions<ProductDataContext> options)
      : base(options)
    {
    }

    public DbSet<Product> Product { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);

        // Configure Product entity for SQL Server
 modelBuilder.Entity<Product>(entity =>
  {
            entity.ToTable("Products", "dbo");
            entity.HasKey(e => e.Id);
   entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
    entity.Property(e => e.Description).HasMaxLength(1000);
       entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
      entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageData).HasColumnType("varbinary(max)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
          entity.Property(e => e.ModifiedDate).HasDefaultValueSql("GETUTCDATE()");

    entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
});
    }
}

public static class Extensions
{
    public static async Task InitializeDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        var logger = services.GetRequiredService<ILogger<ProductDataContext>>();

        try
        {
   // Ensure database exists (for LocalDB)
     await context.Database.EnsureCreatedAsync();
 logger.LogInformation("Database connection successful.");

     // Seed data if empty
    if (!await context.Product.AnyAsync())
    {
     await DbInitializer.InitializeAsync(context, logger);
        }
     }
        catch (Exception ex)
        {
     logger.LogError(ex, "An error occurred while initializing the database.");
          throw;
        }
    }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(ProductDataContext context, ILogger logger)
    {
        logger.LogInformation("Seeding initial product data...");

        var products = new List<Product>
{
            // Note: ImageUrl is set to null - images will be loaded into ImageData via LoadImages.ps1 script
       // This enables Scenario 2: Database image serving via /api/Product/{id}/image
  new Product { Name = "Solar Powered Flashlight", Description = "A fantastic product for outdoor enthusiasts", Price = 19.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Product { Name = "Hiking Poles", Description = "Ideal for camping and hiking trips", Price = 24.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Outdoor Rain Jacket", Description = "This product will keep you warm and dry in all weathers", Price = 49.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
    new Product { Name = "Survival Kit", Description = "A must-have for any outdoor adventurer", Price = 99.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
new Product { Name = "Outdoor Backpack", Description = "This backpack is perfect for carrying all your outdoor essentials", Price = 39.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
      new Product { Name = "Camping Cookware", Description = "This cookware set is ideal for cooking outdoors", Price = 29.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Camping Stove", Description = "This stove is perfect for cooking outdoors", Price = 49.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
     new Product { Name = "Camping Lantern", Description = "This lantern is perfect for lighting up your campsite", Price = 19.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
  new Product { Name = "Camping Tent", Description = "This tent is perfect for camping trips", Price = 99.99m, ImageUrl = null, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        context.Product.AddRange(products);
   await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
        logger.LogInformation("Run LoadImages.ps1 script to load images into ImageData column.");
    }
}