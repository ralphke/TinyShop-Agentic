using System.Data;
using DataEntities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Products.Services;
using System.Text.Json;

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

    public DbSet<AgentCartSession> AgentCartSessions { get; set; } = default!;

    public DbSet<AgentCartItem> AgentCartItems { get; set; } = default!;

    public DbSet<AgentRequestAudit> AgentRequestAudits { get; set; } = default!;

    public async Task UpsertProductVectorsAsync(int productId, float[]? descriptionVector, float[]? nameVector)
    {
        var descriptionJson = descriptionVector is { } desc ? JsonSerializer.Serialize(desc) : null;
        var nameJson = nameVector is { } nameValue ? JsonSerializer.Serialize(nameValue) : null;

        if (descriptionJson is { } description)
        {
            ValidateVectorJson(description, productId, nameof(descriptionVector));
        }

        if (nameJson is { } name)
        {
            ValidateVectorJson(name, productId, nameof(nameVector));
        }

        var connection = (SqlConnection)Database.GetDbConnection();
        var closeConnection = false;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                closeConnection = true;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC dbo.usp_UpsertProductVector @ProductId, @DescriptionVector, @NameVector";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
            command.Parameters.Add(new SqlParameter("@DescriptionVector", SqlDbType.NVarChar, -1) { Value = (object?)descriptionJson ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@NameVector", SqlDbType.NVarChar, -1) { Value = (object?)nameJson ?? DBNull.Value });

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void ValidateVectorJson(string json, int productId, string parameterName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Embedding JSON for product {productId} must be an array, but was {document.RootElement.ValueKind}.");
            }

            var count = 0;
            var allZero = true;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Number)
                {
                    throw new InvalidOperationException($"Embedding JSON for product {productId} contains non-numeric values.");
                }

                count++;
                if (element.GetSingle() != 0f)
                {
                    allZero = false;
                }
            }

            if (count != 768)
            {
                throw new InvalidOperationException($"Embedding JSON for product {productId} must contain 768 values, but contained {count}.");
            }

            if (allZero)
            {
                throw new InvalidOperationException($"Embedding JSON for product {productId} contains an invalid all-zero embedding vector.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON for {parameterName} when updating product {productId}.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            var embeddingConverter = new ValueConverter<float[]?, string?>(
                value => value == null ? null : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<float[]>(value));

            var embeddingComparer = new ValueComparer<float[]?>(
                (left, right) =>
                    left == null && right == null ||
                    left != null && right != null && left.SequenceEqual(right),
                value => value == null ? 0 : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value == null ? null : value.ToArray());

            entity.ToTable("Products", "dbo");
            entity.ToTable(tableBuilder => tableBuilder.UseSqlOutputClause(false));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Details).HasMaxLength(4000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageData).HasColumnType("varbinary(max)");

            if (Database.IsSqlServer())
            {
                entity.Ignore(e => e.DescriptionVector);
                entity.Ignore(e => e.NameVector);
            }
            else
            {
                entity.Property(e => e.DescriptionVector)
                    .HasConversion(embeddingConverter, embeddingComparer)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.NameVector)
                    .HasConversion(embeddingConverter, embeddingComparer)
                    .HasColumnType("nvarchar(max)");
            }

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

        modelBuilder.Entity<AgentCartSession>(entity =>
        {
            entity.ToTable("AgentCartSessions", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentId).IsRequired().HasMaxLength(128);
            entity.HasIndex(e => new { e.AgentId, e.CustomerId }).HasDatabaseName("IX_AgentCartSessions_Agent_Customer");
            entity.HasMany(e => e.Items)
                .WithOne(e => e.CartSession)
                .HasForeignKey(e => e.CartSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentCartItem>(entity =>
        {
            entity.ToTable("AgentCartItems", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.CartSessionId, e.ProductId }).IsUnique().HasDatabaseName("UX_AgentCartItems_Session_Product");
        });

        modelBuilder.Entity<AgentRequestAudit>(entity =>
        {
            entity.ToTable("AgentRequestAudits", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AgentId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(80);
            entity.Property(e => e.RequestId).HasMaxLength(64);
            entity.HasIndex(e => new { e.AgentId, e.CreatedDate }).HasDatabaseName("IX_AgentRequestAudits_Agent_Created");
        });
    }
}

public static class Extensions
{
    public static async Task InitializeDatabaseAsync(this IHost host)
        => await InitializeDatabaseAsync(host, createIfMissing: true);

    public static async Task InitializeDatabaseAsync(this IHost host, bool createIfMissing)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var logger = services.GetRequiredService<ILogger<ProductDataContext>>();

        try
        {
            if (createIfMissing)
            {
                await context.Database.EnsureCreatedAsync();
            }
            else if (!await context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException("Database is not available for maintenance.");
            }

            if (context.Database.IsSqlServer())
            {
                await EnsureShopTablesAsync(context);
            }

            await EnsureProductDetailsAsync(context);

            logger.LogInformation("Database connection successful.");

            if (!await context.Product.AnyAsync())
            {
                await DbInitializer.InitializeAsync(context, logger, embeddingService);
            }
            else
            {
                await DbInitializer.LoadImagesAsync(context, logger);
            }

            await EnsureProductEmbeddingsAsync(context, embeddingService, logger);

            await EnsureVectorIndexAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public static async Task EnsureDatabaseMaintenanceAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var logger = services.GetRequiredService<ILogger<ProductDataContext>>();

        try
        {
            if (!await context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException("Database is not available for maintenance.");
            }

            if (context.Database.IsSqlServer())
            {
                await EnsureShopTablesAsync(context);
            }

            await EnsureProductDetailsAsync(context);

            logger.LogInformation("Database maintenance started.");

            if (!await context.Product.AnyAsync())
            {
                logger.LogWarning("No products found in database. Skipping data initialization because the database was provisioned externally.");
                return;
            }

            await EnsureProductEmbeddingsAsync(context, embeddingService, logger);
            await EnsureVectorIndexAsync(context, logger);
            await DbInitializer.LoadImagesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while maintaining the database.");
            throw;
        }
    }

    public static async Task GenerateProductEmbeddingsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProductDataContext>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var logger = services.GetRequiredService<ILogger<ProductDataContext>>();

        if (!await context.Database.CanConnectAsync())
        {
            throw new InvalidOperationException("Database is not available for embedding generation.");
        }

        await EnsureProductEmbeddingsAsync(context, embeddingService, logger);
    }

    private static async Task EnsureShopTablesAsync(ProductDataContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.Products', N'U') IS NULL 
            BEGIN
                CREATE TABLE dbo.Products
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(1000) NULL,
                    Details NVARCHAR(4000) NULL,
                    Price DECIMAL(18,2) NOT NULL,
                    ImageUrl NVARCHAR(500) NULL,
                    ImageData VARBINARY(MAX) NULL,
                    DescriptionVector VECTOR(768, FLOAT32) NULL,
                    NameVector VECTOR(768, FLOAT32) NULL,
                    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedDate DEFAULT SYSUTCDATETIME(),
                    ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_Products_ModifiedDate DEFAULT SYSUTCDATETIME()
                );
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'Products' AND COLUMN_NAME = N'DescriptionVector')
                BEGIN
                    ALTER TABLE dbo.Products DROP COLUMN DescriptionVector;
                END;

                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'Products' AND COLUMN_NAME = N'NameVector')
                BEGIN
                    ALTER TABLE dbo.Products DROP COLUMN NameVector;
                END;

                ALTER TABLE dbo.Products ADD DescriptionVector VECTOR(768, FLOAT32) NULL;
                ALTER TABLE dbo.Products ADD NameVector VECTOR(768, FLOAT32) NULL;
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON [dbo].[Products];' + CHAR(10)
            FROM sys.indexes AS i
            WHERE i.object_id = OBJECT_ID(N'dbo.Products')
              AND i.type_desc LIKE N'%VECTOR%';

            IF LEN(@sql) > 0
            BEGIN
                EXEC sp_executesql @sql;
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

        await context.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.AgentCartSessions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AgentCartSessions
                (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    AgentId NVARCHAR(128) NOT NULL,
                    CustomerId INT NOT NULL,
                    CreatedDate DATETIME2 NOT NULL,
                    LastActivityDate DATETIME2 NOT NULL,
                    ExpiresAt DATETIME2 NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.AgentCartItems', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AgentCartItems
                (
                    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CartSessionId UNIQUEIDENTIFIER NOT NULL,
                    ProductId INT NOT NULL,
                    ProductName NVARCHAR(200) NOT NULL,
                    UnitPrice DECIMAL(18,2) NOT NULL,
                    Quantity INT NOT NULL,
                    CONSTRAINT FK_AgentCartItems_AgentCartSessions FOREIGN KEY (CartSessionId) REFERENCES dbo.AgentCartSessions(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.AgentRequestAudits', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AgentRequestAudits
                (
                    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    AgentId NVARCHAR(128) NOT NULL,
                    CustomerId INT NOT NULL,
                    Operation NVARCHAR(80) NOT NULL,
                    RequestId NVARCHAR(64) NULL,
                    StatusCode INT NOT NULL,
                    CreatedDate DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentCartSessions_Agent_Customer' AND object_id = OBJECT_ID(N'dbo.AgentCartSessions'))
            BEGIN
                CREATE INDEX IX_AgentCartSessions_Agent_Customer ON dbo.AgentCartSessions(AgentId, CustomerId);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AgentCartItems_Session_Product' AND object_id = OBJECT_ID(N'dbo.AgentCartItems'))
            BEGIN
                CREATE UNIQUE INDEX UX_AgentCartItems_Session_Product ON dbo.AgentCartItems(CartSessionId, ProductId);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AgentRequestAudits_Agent_Created' AND object_id = OBJECT_ID(N'dbo.AgentRequestAudits'))
            BEGIN
                CREATE INDEX IX_AgentRequestAudits_Agent_Created ON dbo.AgentRequestAudits(AgentId, CreatedDate);
            END;
            """);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE OR ALTER PROCEDURE dbo.usp_UpsertProductVector
                @ProductId INT,
                @DescriptionVector NVARCHAR(MAX) = NULL,
                @NameVector NVARCHAR(MAX) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE dbo.Products
                SET
                    DescriptionVector = CASE WHEN @DescriptionVector IS NOT NULL THEN CAST(@DescriptionVector AS VECTOR(768, FLOAT32)) ELSE DescriptionVector END,
                    NameVector = CASE WHEN @NameVector IS NOT NULL THEN CAST(@NameVector AS VECTOR(768, FLOAT32)) ELSE NameVector END
                WHERE Id = @ProductId;
            END;
            """);
    }

    public static async Task<List<int>> GetProductIdsMissingEmbeddingsAsync(this ProductDataContext context)
    {
        if (!context.Database.IsSqlServer())
        {
            return await context.Product
                .Where(product => product.DescriptionVector == null || product.NameVector == null)
                .Select(product => product.Id)
                .ToListAsync();
        }

        var ids = new List<int>();
        var connection = context.Database.GetDbConnection();
        var closeConnection = false;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                closeConnection = true;
            }

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Id,
                       CAST(DescriptionVector AS NVARCHAR(MAX)) AS DescriptionVector,
                       CAST(NameVector AS NVARCHAR(MAX)) AS NameVector
                FROM dbo.Products;
                """;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var productId = reader.GetInt32(0);
                var descriptionJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                var nameJson = reader.IsDBNull(2) ? null : reader.GetString(2);

                if (!HasValidEmbedding(descriptionJson) || !HasValidEmbedding(nameJson))
                {
                    ids.Add(productId);
                }
            }
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        return ids;
    }

    private static bool HasValidEmbedding(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var vector = JsonSerializer.Deserialize<float[]>(json);
            return vector != null && vector.Length == 768 && vector.Any(value => value != 0f);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> HasAnyEmbeddingsAsync(this ProductDataContext context)
    {
        if (!context.Database.IsSqlServer())
        {
            return await context.Product.AnyAsync(product => product.DescriptionVector != null);
        }

        var connection = context.Database.GetDbConnection();
        var closeConnection = false;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                closeConnection = true;
            }

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Id,
                       CAST(DescriptionVector AS NVARCHAR(MAX)) AS DescriptionVector,
                       CAST(NameVector AS NVARCHAR(MAX)) AS NameVector
                FROM dbo.Products;
                """;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var descriptionJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                var nameJson = reader.IsDBNull(2) ? null : reader.GetString(2);
                if (HasValidEmbedding(descriptionJson) || HasValidEmbedding(nameJson))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static async Task<List<Product>> LoadProductsWithVectorsAsync(this ProductDataContext context)
    {
        if (!context.Database.IsSqlServer())
        {
            return await context.Product.ToListAsync();
        }

        var products = await context.Product.ToListAsync();
        var vectorMap = new Dictionary<int, (string? Description, string? Name)>();

        var connection = context.Database.GetDbConnection();
        var closeConnection = false;

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                closeConnection = true;
            }

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Id,
                       CAST(DescriptionVector AS NVARCHAR(MAX)) AS DescriptionVector,
                       CAST(NameVector AS NVARCHAR(MAX)) AS NameVector
                FROM dbo.Products;
                """;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var descriptionJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                var nameJson = reader.IsDBNull(2) ? null : reader.GetString(2);
                vectorMap[id] = (descriptionJson, nameJson);
            }
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        foreach (var product in products)
        {
            if (vectorMap.TryGetValue(product.Id, out var vectorJson))
            {
                product.DescriptionVector = vectorJson.Description is null ? null : JsonSerializer.Deserialize<float[]>(vectorJson.Description);
                product.NameVector = vectorJson.Name is null ? null : JsonSerializer.Deserialize<float[]>(vectorJson.Name);
            }
        }

        return products;
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

    private static async Task EnsureProductEmbeddingsAsync(ProductDataContext context, IEmbeddingService embeddingService, ILogger logger)
    {
        var missingIds = await context.GetProductIdsMissingEmbeddingsAsync();
        if (missingIds.Count == 0)
        {
            return;
        }

        var productsNeedingEmbeddings = await context.Product
            .Where(product => missingIds.Contains(product.Id))
            .ToListAsync();

        foreach (var product in productsNeedingEmbeddings)
        {
            await GenerateProductEmbeddingsAsync(product, embeddingService, logger);
        }

        await context.SaveChangesAsync();

        foreach (var product in productsNeedingEmbeddings)
        {
            await context.UpsertProductVectorsAsync(product.Id, product.DescriptionVector, product.NameVector);
        }
    }

    private static async Task GenerateProductEmbeddingsAsync(Product product, IEmbeddingService embeddingService, ILogger logger)
    {
        try
        {
            if (product.DescriptionVector == null)
            {
                var descriptionText = BuildEmbeddingText(product);
                if (!string.IsNullOrWhiteSpace(descriptionText))
                {
                    product.DescriptionVector = await embeddingService.EmbedTextAsync(descriptionText);
                }
            }

            if (product.NameVector == null)
            {
                var nameText = BuildNameEmbeddingText(product);
                if (!string.IsNullOrWhiteSpace(nameText))
                {
                    product.NameVector = await embeddingService.EmbedTextAsync(nameText);
                }
            }

            product.ModifiedDate = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embeddings for product {ProductId}", product.Id);
        }
    }

    private static async Task EnsureVectorIndexAsync(ProductDataContext context, ILogger logger)
    {
        try
        {
            // For SQL Server 2025+ with native VECTOR type support, a VECTOR index would be created here.
            
            logger.LogInformation("Vector embeddings are persisted and ready for semantic search via in-memory similarity scoring.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during vector index verification. Semantic search will use in-memory similarity scoring.");
        }
    }

    private static string BuildEmbeddingText(Product product)
    {
        return string.Join(' ', new[] { product.Description, product.Details }.Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private static string BuildNameEmbeddingText(Product product)
    {
        return string.IsNullOrWhiteSpace(product.Name) ? string.Empty : product.Name;
    }
}

public static class DbInitializer
{
    public static async Task InitializeAsync(ProductDataContext context, ILogger logger, IEmbeddingService embeddingService)
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

        foreach (var product in products)
        {
            product.DescriptionVector = await embeddingService.EmbedTextAsync(BuildEmbeddingText(product));
            product.NameVector = await embeddingService.EmbedTextAsync(BuildNameEmbeddingText(product));
        }

        context.Product.AddRange(products);
        await context.SaveChangesAsync();

        foreach (var product in products)
        {
            await context.UpsertProductVectorsAsync(product.Id, product.DescriptionVector, product.NameVector);
        }

        await LoadImagesAsync(context, logger);

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
        logger.LogInformation("Images loaded into ImageData column.");
    }

    private static string BuildEmbeddingText(Product product)
    {
        return string.Join(' ', new[] { product.Description, product.Details }.Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private static string BuildNameEmbeddingText(Product product)
    {
        return string.IsNullOrWhiteSpace(product.Name) ? string.Empty : product.Name;
    }

    internal static async Task LoadImagesAsync(ProductDataContext context, ILogger logger)
    {
        try
        {
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(imagesPath))
            {
                var altPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images");
                if (Directory.Exists(altPath))
                {
                    imagesPath = altPath;
                }
                else
                {
                    logger.LogWarning("Images directory not found: {Path} or {AltPath}", imagesPath, altPath);
                    return;
                }
            }

            var products = await context.Product.OrderBy(p => p.Id).ToListAsync();
            var changed = false;
            foreach (var product in products)
            {
                if (product.ImageData != null && product.ImageData.Length > 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    logger.LogWarning("Product {Id} has no ImageUrl configured, skipping image load.", product.Id);
                    continue;
                }

                var imageRelativePath = product.ImageUrl.TrimStart('/', '\\');
                if (imageRelativePath.StartsWith("images" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    imageRelativePath.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ||
                    imageRelativePath.StartsWith("images\\", StringComparison.OrdinalIgnoreCase))
                {
                    imageRelativePath = imageRelativePath.Substring("images".Length).TrimStart('/', '\\');
                }

                var imageFile = Path.Combine(imagesPath, imageRelativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
                if (!File.Exists(imageFile))
                {
                    logger.LogWarning("Image file not found for product {Id}: {File}", product.Id, imageFile);
                    continue;
                }

                try
                {
                    product.ImageData = await File.ReadAllBytesAsync(imageFile);
                    changed = true;
                    logger.LogInformation("Loaded image for product {Id}: {File}", product.Id, imageFile);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load image: {File}", imageFile);
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync();
            }
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
