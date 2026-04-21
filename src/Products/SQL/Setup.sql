-- SQL Server Setup Script for TinyShopDB
-- Run this script via sqlcmd in SQLCMD mode, passing the password from .env:
--
--   PowerShell against LocalDB:
--     $pw = (Get-Content .env | Select-String 'MSSQL_SA_PASSWORD').ToString().Split('=',2)[1]
--     sqlcmd -S "(localdb)\MSSQLLocalDB" -i src/Products/SQL/Setup.sql -v MSSQL_SA_PASSWORD="$pw"
--
--   PowerShell against SQL container:
--     $pw = (Get-Content .env | Select-String 'MSSQL_SA_PASSWORD').ToString().Split('=',2)[1]
--     sqlcmd -S localhost,1433 -U sa -P "$pw" -i src/Products/SQL/Setup.sql
--
--   cmd against SQL container:
--     for /f "tokens=2 delims==" %i in ('findstr MSSQL_SA_PASSWORD .env') do set _PW=%i
--     sqlcmd -S localhost,1433 -U sa -P "%_PW%" -i src/Products/SQL/Setup.sql
--
-- The MSSQL_SA_PASSWORD value is defined in the .env file at the repository root.
--
-- This script creates TinyShopDB, the application schema, the application login/user,
-- and initial product data compatible with the app.

SET NOCOUNT ON;
USE MASTER;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'TinyShopDB')
    BEGIN
    ALTER DATABASE TinyShopDB
        SET SINGLE_USER
        WITH ROLLBACK IMMEDIATE;

    DROP DATABASE TinyShopDB;  
END

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TinyShopDB')
BEGIN
    CREATE DATABASE TinyShopDB  COLLATE Latin1_General_100_BIN2_UTF8
END
GO

USE TinyShopDB;
GO

IF NOT EXISTS (SELECT name
FROM sys.server_principals
WHERE name = 'TinyShopUser')
BEGIN
    CREATE LOGIN TinyShopUser WITH PASSWORD = 'P@ssw0rd123!', CHECK_POLICY = OFF;
END
GO

IF NOT EXISTS (SELECT name
FROM sys.database_principals
WHERE name = 'TinyShopUser')
BEGIN
    CREATE USER TinyShopUser FOR LOGIN TinyShopUser;
END
GO

ALTER ROLE db_datareader ADD MEMBER TinyShopUser;
ALTER ROLE db_datawriter ADD MEMBER TinyShopUser;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,Name NVARCHAR(200) NOT NULL
        ,Description NVARCHAR(1000) NULL
        ,Details NVARCHAR(4000) NULL
        ,Price DECIMAL(18,2) NOT NULL
        ,ImageUrl NVARCHAR(500) NULL
        ,ImageData VARBINARY(MAX) NULL
        ,CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
        ,ModifiedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
        ,DescriptionVector VECTOR(768) NULL
        ,NameVector VECTOR(768) NULL
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_Products_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_Name ON dbo.Products(Name);
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_Products_Price' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_Price ON dbo.Products(Price);
END
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,Name NVARCHAR(200) NOT NULL
        ,Email NVARCHAR(320) NOT NULL
        ,Address NVARCHAR(1000) NOT NULL
        ,PasswordHash NVARCHAR(255) NOT NULL
        ,CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
        ,ModifiedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'UX_Customers_Email' AND object_id = OBJECT_ID(N'dbo.Customers'))
BEGIN
    CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers(Email);
END
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,CustomerId INT NULL
        ,CustomerName NVARCHAR(200) NOT NULL
        ,CustomerEmail NVARCHAR(320) NOT NULL
        ,ShippingAddress NVARCHAR(1000) NOT NULL
        ,Status NVARCHAR(50) NOT NULL
        ,Total DECIMAL(18,2) NOT NULL
        ,CreatedDate DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
        ,CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_Orders_CustomerId' AND object_id = OBJECT_ID(N'dbo.Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
END
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,OrderId INT NOT NULL
        ,ProductId INT NOT NULL
        ,ProductName NVARCHAR(200) NOT NULL
        ,UnitPrice DECIMAL(18,2) NOT NULL
        ,Quantity INT NOT NULL
        ,CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
GO

IF OBJECT_ID(N'dbo.AgentCartSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentCartSessions
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
        ,AgentId NVARCHAR(128) NOT NULL
        ,CustomerId INT NOT NULL
        ,CreatedDate DATETIME2(7) NOT NULL
        ,LastActivityDate DATETIME2(7) NOT NULL
        ,ExpiresAt DATETIME2(7) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_AgentCartSessions_Agent_Customer' AND object_id = OBJECT_ID(N'dbo.AgentCartSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentCartSessions_Agent_Customer ON dbo.AgentCartSessions(AgentId, CustomerId);
END
GO

IF OBJECT_ID(N'dbo.AgentCartItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentCartItems
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,CartSessionId UNIQUEIDENTIFIER NOT NULL
        ,ProductId INT NOT NULL
        ,ProductName NVARCHAR(200) NOT NULL
        ,UnitPrice DECIMAL(18,2) NOT NULL
        ,Quantity INT NOT NULL
        ,CONSTRAINT FK_AgentCartItems_AgentCartSessions FOREIGN KEY (CartSessionId) REFERENCES dbo.AgentCartSessions(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'UX_AgentCartItems_Session_Product' AND object_id = OBJECT_ID(N'dbo.AgentCartItems'))
BEGIN
    CREATE UNIQUE INDEX UX_AgentCartItems_Session_Product ON dbo.AgentCartItems(CartSessionId, ProductId);
END
GO

IF OBJECT_ID(N'dbo.AgentRequestAudits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentRequestAudits
    (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY
        ,AgentId NVARCHAR(128) NOT NULL
        ,CustomerId INT NOT NULL
        ,Operation NVARCHAR(80) NOT NULL
        ,RequestId NVARCHAR(64) NULL
        ,StatusCode INT NOT NULL
        ,CreatedDate DATETIME2(7) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT *
FROM sys.indexes
WHERE name = 'IX_AgentRequestAudits_Agent_Created' AND object_id = OBJECT_ID(N'dbo.AgentRequestAudits'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentRequestAudits_Agent_Created ON dbo.AgentRequestAudits(AgentId, CreatedDate);
END
GO

Print 'Database schema setup complete. Now inserting initial product data...';
GO

:r Products_Inserts.sql
:r LoadImages.sql
PRINT 'Initial product data inserted. Now adding vector support and testing embedding integration...';
GO

-- Note: The following vector support scripts are optional and may fail if not needed
-- :r usp_EnableVectorSupport.sql
-- :r usp_UpsertProductVector.sql
-- :r usp_CreateProductVectorIndexes.sql
-- :r usp_HybridProductSearch.sql
-- :r usp_SearchProductsBySimilarity.sql


PRINT 'TinyShopDB provisioning complete. Run the application after the database is created.';
GO
