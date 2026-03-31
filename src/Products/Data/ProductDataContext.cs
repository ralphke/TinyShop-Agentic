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

    public DbSet<CustomerProfile> Customers { get; set; } = default!;

    public DbSet<Order> Orders { get; set; } = default!;

    public DbSet<OrderItem> OrderItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Details).HasMaxLength(4000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageData).HasColumnType("varbinary(max)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
            entity.HasIndex(e => e.Price).HasDatabaseName("IX_Products_Price");
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.ToTable("Customers", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Customers_Email");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(320);
            entity.Property(e => e.ShippingAddress).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Customer)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CustomerId).HasDatabaseName("IX_Orders_CustomerId");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
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
            await context.Database.EnsureCreatedAsync();

            if (context.Database.IsSqlServer())
            {
                await EnsureShopTablesAsync(context);
            }

            await EnsureProductDetailsAsync(context);

            logger.LogInformation("Database connection successful.");

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

    private static async Task EnsureShopTablesAsync(ProductDataContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH(N'dbo.Products', N'Details') IS NULL
            BEGIN
                ALTER TABLE dbo.Products ADD Details NVARCHAR(4000) NULL;
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Customers
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Email NVARCHAR(320) NOT NULL,
                    Address NVARCHAR(1000) NOT NULL,
                    PasswordHash NVARCHAR(255) NOT NULL,
                    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedDate DEFAULT SYSUTCDATETIME(),
                    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Customers_ModifiedDate DEFAULT SYSUTCDATETIME()
                );
            END
            ELSE
            BEGIN
                -- Add PasswordHash column if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'PasswordHash')
                BEGIN
                    ALTER TABLE dbo.Customers ADD PasswordHash NVARCHAR(255) NOT NULL DEFAULT '';
                END;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Customers_Email' AND object_id = OBJECT_ID(N'dbo.Customers'))
            BEGIN
                CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers(Email);
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Orders
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CustomerId INT NULL,
                    CustomerName NVARCHAR(200) NOT NULL,
                    CustomerEmail NVARCHAR(320) NOT NULL,
                    ShippingAddress NVARCHAR(1000) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    Total DECIMAL(18,2) NOT NULL,
                    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedDate DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id) ON DELETE SET NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId' AND object_id = OBJECT_ID(N'dbo.Orders'))
            BEGIN
                CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.OrderItems
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductId INT NOT NULL,
                    ProductName NVARCHAR(200) NOT NULL,
                    UnitPrice DECIMAL(18,2) NOT NULL,
                    Quantity INT NOT NULL,
                    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
            BEGIN
                CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
            END;
            """);
    }

    private static async Task EnsureProductDetailsAsync(ProductDataContext context)
    {
        var productsToUpdate = await context.Product
            .Where(product => string.IsNullOrWhiteSpace(product.Details))
            .ToListAsync();

        if (productsToUpdate.Count == 0)
        {
            return;
        }

        foreach (var product in productsToUpdate)
        {
            product.Details = ProductCatalogContent.GetDetails(product.Name);
            product.ModifiedDate = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(ProductDataContext context, ILogger logger)
    {
        logger.LogInformation("Seeding initial product data...");

        var products = new List<Product>
        {
            new() { Name = "Solar Powered Flashlight", Description = "A compact trail light that keeps working after a full day in the sun.", Details = ProductCatalogContent.GetDetails("Solar Powered Flashlight"), Price = 19.99m, ImageUrl = "images/product1.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Hiking Poles", Description = "Lightweight support poles that reduce fatigue on long climbs and uneven paths.", Details = ProductCatalogContent.GetDetails("Hiking Poles"), Price = 24.99m, ImageUrl = "images/product2.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Outdoor Rain Jacket", Description = "A weather-ready shell that blocks wind and rain without feeling bulky.", Details = ProductCatalogContent.GetDetails("Outdoor Rain Jacket"), Price = 49.99m, ImageUrl = "images/product3.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Survival Kit", Description = "A grab-and-go emergency pack built for backcountry trips and unexpected delays.", Details = ProductCatalogContent.GetDetails("Survival Kit"), Price = 99.99m, ImageUrl = "images/product4.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Outdoor Backpack", Description = "A versatile day-pack with enough room for layers, water, and camp essentials.", Details = ProductCatalogContent.GetDetails("Outdoor Backpack"), Price = 39.99m, ImageUrl = "images/product5.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Camping Cookware", Description = "A nesting cookware set sized for simple meals at the campsite.", Details = ProductCatalogContent.GetDetails("Camping Cookware"), Price = 29.99m, ImageUrl = "images/product6.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Camping Stove", Description = "A dependable stove for fast boils, trail coffee, and compact camp kitchens.", Details = ProductCatalogContent.GetDetails("Camping Stove"), Price = 49.99m, ImageUrl = "images/product7.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Camping Lantern", Description = "A bright campsite lantern that adds warm light to tents, tables, and trails.", Details = ProductCatalogContent.GetDetails("Camping Lantern"), Price = 19.99m, ImageUrl = "images/product8.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new() { Name = "Camping Tent", Description = "A roomy shelter designed for quick setup and reliable overnight protection.", Details = ProductCatalogContent.GetDetails("Camping Tent"), Price = 99.99m, ImageUrl = "images/product9.png", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        context.Product.AddRange(products);
        await context.SaveChangesAsync();

        await LoadImagesAsync(context, logger);

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
        logger.LogInformation("Images loaded into ImageData column.");
    }

    private static async Task LoadImagesAsync(ProductDataContext context, ILogger logger)
    {
        try
        {
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(imagesPath))
            {
                logger.LogWarning("Images directory not found: {Path}", imagesPath);
                return;
            }

            var products = await context.Product.OrderBy(p => p.Id).ToListAsync();
            for (var index = 0; index < products.Count && index < 9; index++)
            {
                var imageFile = Path.Combine(imagesPath, $"product{index + 1}.png");
                if (!File.Exists(imageFile))
                {
                    logger.LogWarning("Image file not found: {File}", imageFile);
                    continue;
                }

                try
                {
                    products[index].ImageData = await File.ReadAllBytesAsync(imageFile);
                    logger.LogInformation("Loaded image for product {Id}: {File}", products[index].Id, imageFile);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load image: {File}", imageFile);
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during image loading - continuing without images");
        }
    }
}

internal static class ProductCatalogContent
{
    private static readonly Dictionary<string, string> DetailsByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Solar Powered Flashlight"] = "Built for campsites, late hikes, and glove-box backups, this flashlight stores solar power through the day and throws a focused beam when you need reliable visibility after dark. It is a strong choice for travelers who want lightweight gear with fewer disposable batteries to manage. Pairs especially well with the Survival Kit, Camping Lantern, and Hiking Poles for a safer after-sunset setup.",
        ["Hiking Poles"] = "These poles add balance on rocky switchbacks, take pressure off knees during descents, and help you keep a steady pace over long distances. They are ideal for day hikes and multi-stop treks where comfort matters as much as speed. They work particularly well with the Outdoor Rain Jacket, Outdoor Backpack, and Camping Tent for full-day trail coverage.",
        ["Outdoor Rain Jacket"] = "This jacket is a lightweight outer layer for gusty ridgelines, sudden showers, and cold early starts. It gives you enough weather protection to keep moving without overheating or packing a heavy shell. It complements the Hiking Poles, Outdoor Backpack, and Camping Tent when conditions look unpredictable.",
        ["Survival Kit"] = "Packed for preparedness, this kit covers the small essentials that become critical when weather shifts, routes change, or camp setup takes longer than planned. It is designed to sit in your pack without adding much mental overhead because the basics are already handled. It pairs naturally with the Solar Powered Flashlight, Camping Lantern, and Outdoor Backpack.",
        ["Outdoor Backpack"] = "Sized for day trips and minimalist overnighters, this backpack keeps food, layers, and utility gear organized without feeling oversized. It is the anchor item for customers building a practical outdoor loadout around flexibility and comfort. It goes especially well with the Hiking Poles, Outdoor Rain Jacket, and Camping Cookware.",
        ["Camping Cookware"] = "This cookware set nests neatly for transport and gives you the basics for hot drinks, quick breakfasts, and one-pot meals at camp. It is a strong fit for travelers who want simple meal prep without bulky kitchen gear. It works best alongside the Camping Stove, Camping Lantern, and Camping Tent.",
        ["Camping Stove"] = "Designed for compact cooking, this stove delivers quick heat for boiling water and preparing trail meals without a large setup. It is a dependable choice for campers who want speed, control, and a smaller footprint in their pack. It matches well with the Camping Cookware, Camping Lantern, and Camping Tent.",
        ["Camping Lantern"] = "This lantern adds broad, comfortable light for evening meals, tent organization, and campsite safety once the sun drops. It is useful when a flashlight is too directional and you need shared visibility around camp. It pairs neatly with the Solar Powered Flashlight, Camping Stove, and Camping Tent.",
        ["Camping Tent"] = "A solid tent turns a campsite into a base camp, and this one is positioned as the shelter piece around which the rest of an overnight setup comes together. It is aimed at campers who want straightforward setup and dependable coverage from dusk to dawn. It teams up well with the Camping Lantern, Outdoor Backpack, and Camping Cookware."
    };

    public static string GetDetails(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return "A dependable outdoor item chosen to make camping and hiking easier, more comfortable, and better prepared.";
        }

        return DetailsByName.TryGetValue(productName, out var details)
            ? details
            : $"{productName} is part of the TinyShop outdoor range and is designed to fit naturally into a practical camping or hiking setup.";
    }
}
