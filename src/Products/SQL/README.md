# SQL Server Setup Instructions

## Prerequisites

- SQL Server 2025:latest Docker image installed (included with Visual Studio or SQL Server Express)
- SQL Server Management Studio (SSMS) or Visual Studio Code mssql extension (optional, for running SQL scripts)

## Setup Steps

### Option 1: Using SQL Authentication (Recommended for sql in Docker container)

1. **Run the SQL Setup Script**
   - Open SQL Server Management Studio (SSMS) or Azure Data Studio
   - Connect to `sqlserver:1433`
   - Open the file `Products/SQL/Setup.sql`
   - Execute the script

   **OR** use sqlcmd from command line, passing the password from `.env`:

   PowerShell:

   ```powershell
   $pw = (Get-Content .env | Select-String 'MSSQL_SA_PASSWORD').ToString().Split('=',2)[1]
   sqlcmd -S "sqlserver:1433" -i src\Products\SQL\Setup.sql -v MSSQL_SA_PASSWORD="$pw"
   ```

   cmd:

   ```cmd
   for /f "tokens=2 delims==" %i in ('findstr MSSQL_SA_PASSWORD .env') do set _PW=%i
   sqlcmd -S sqlserver -i src\Products\SQL\Setup.sql -v MSSQL_SA_PASSWORD="%_PW%"
   ```

2. **The script will:**
   - Create the `TestDB` database
   - Create the `dbo.Products` table with binary image support
   - Create a SQL Server login `TinyShopUser` (password read from `MSSQL_SA_PASSWORD` in `.env`)
   - Grant necessary permissions
   - Seed initial product data

3. **Configure Connection String**
   The application is configured to use **SQL Server Security** by default:

   ```json
   "ConnectionStrings": {
  "TinyShopDB": "Server=sqlserver;Database=TinyShopDB;Integrated Security=false;TrustServerCertificate=True;"
   }
   ```

   If you want to use **SQL Server Authentication**, update `appsettings.Development.json`,
   replacing `<password>` with the value of `MSSQL_SA_PASSWORD` from `.env`:
   ```json
   "ConnectionStrings": {
     "TinyShopDB": "Server=sqlserver;Database=TinyShopDB;User Id=TinyShopUser;Password=<password>;TrustServerCertificate=True;"
   }
   ```

### Option 2: Automatic Database Creation

The application will automatically create the database and seed data on first run if using Integrated Security:

1. Ensure sqlserver is running
2. Run the application
3. EF Core will call `EnsureCreatedAsync()` to create the database

## Working with Binary Images

### Loading Images into the Database

You can load images using the API endpoint:

```bash
# Upload an image for product ID 1
curl -X PUT "https://localhost:PORT/api/Product/1/image" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@path/to/image.png"
```

### Retrieving Images

Images are returned as base64-encoded data in the JSON response:

```json
{
  "id": 1,
  "name": "Solar Powered Flashlight",
  "imageDataBase64": "iVBORw0KGgoAAAANSUhEUgAA..."
}
```

Or retrieve the image directly:
```
GET /api/Product/1/image
```

## Troubleshooting

### sqlserver Not Found
If you get "Cannot connect to sqlserver":
- Verify SQLServer is installed: `sqllocaldb info`
- Start sqlserver: `sqllocaldb start MSSQLLocalDB`
- Check version: `sqllocaldb versions`

### Login Failed
If using SQL Authentication and login fails:
- Ensure SQL Server Authentication is enabled

### Connection String Issues
- Always escape backslashes in JSON: `sqlserver:1433`
- Use `TrustServerCertificate=True` for SQlserver

## Database Schema

```sql
CREATE TABLE dbo.Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Price DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    ImageData VARBINARY(MAX) NULL,
    DescriptionVector vector(768) NULL,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 DEFAULT GETUTCDATE()
);
```

## Testing the Setup

Run the Products API and test the endpoints:

```bash
# Get all products
curl https://localhost:PORT/api/Product

# Get a specific product with image data
curl https://localhost:PORT/api/Product/1

# Get product image as PNG
curl https://localhost:PORT/api/Product/1/image --output product1.png
```
