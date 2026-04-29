Integration tests for TinyShop (Products API and Store UI)

Run tests:

dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj

If SQL Server is running outside the test host process (for example via Aspire or docker compose), set the connection string explicitly before running tests:

PowerShell:

$env:ConnectionStrings__TinyShopDB = "Server=localhost,1433;Database=TinyShopDB;User Id=sa;Password=P@ssw0rd123!;TrustServerCertificate=True;Encrypt=False;"
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
