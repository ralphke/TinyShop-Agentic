create database TestDB;
go
use TestDB;
go

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
    DROP TABLE dbo.Products;
GO

Create Table Products
(
	Id int primary key,
	name varchar(50),
	Description varchar(255),
	Price decimal(10,2),
	ImageUrl varchar(255),
	image varbinary(max)
);
GO

BEGIN TRANSACTION;

INSERT INTO Products (Id, name, Description, Price, ImageUrl, image) VALUES
(1, 'Solar Powered Flashlight', 'A fantastic product for outdoor enthusiasts', 19.99, 'product1.png', NULL),
(2, 'Hiking Poles', 'Ideal for camping and hiking trips', 24.99, 'product2.png', NULL),
(3, 'Outdoor Rain Jacket', 'This product will keep you warm and dry in all weathers', 49.99, 'product3.png', NULL),
(4, 'Survival Kit', 'A must-have for any outdoor adventurer', 99.99, 'product4.png', NULL),
(5, 'Outdoor Backpack', 'This backpack is perfect for carrying all your outdoor essentials', 39.99, 'product5.png', NULL),
(6, 'Camping Cookware', 'This cookware set is ideal for cooking outdoors', 29.99, 'product6.png', NULL),
(7, 'Camping Stove', 'This stove is perfect for cooking outdoors', 49.99, 'product7.png', NULL),
(8, 'Camping Lantern', 'This lantern is perfect for lighting up your campsite', 19.99, 'product8.png', NULL),
(9, 'Camping Tent', 'This tent is perfect for camping trips', 99.99, 'product9.png', NULL);

COMMIT;
GO

-- Attempt to enable Ad Hoc Distributed Queries (requires sysadmin)
BEGIN TRY
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE WITH OVERRIDE;
    EXEC sp_configure 'Ad Hoc Distributed Queries', 1;
    RECONFIGURE WITH OVERRIDE;
END TRY
BEGIN CATCH
    -- If enabling fails, continue - the OPENROWSET calls may still work if already enabled.
    PRINT 'Warning: Could not enable Ad Hoc Distributed Queries in this session or insufficient permissions.';
    PRINT ERROR_MESSAGE();
END CATCH
GO

-- Base folder where images are stored on the SQL Server machine.
DECLARE @BasePath nvarchar(4000) = N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\';

-- Use dynamic SQL per-row to pass a literal path into OPENROWSET(BULK ...)
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Id int;
    DECLARE @ImageUrl nvarchar(255);
    DECLARE @FullPath nvarchar(4000);
    DECLARE @sql nvarchar(max);

    DECLARE image_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, ImageUrl FROM Products WHERE ImageUrl IS NOT NULL;

    OPEN image_cursor;
    FETCH NEXT FROM image_cursor INTO @Id, @ImageUrl;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Build full path and escape single quotes
        SET @FullPath = @BasePath + ISNULL(@ImageUrl, '');
        SET @FullPath = REPLACE(@FullPath, '''', '''''');

        -- Build dynamic SQL using a string literal for OPENROWSET path
        SET @sql = N'
            UPDATE Products
            SET image = (
                SELECT BulkColumn
                FROM OPENROWSET(BULK N''' + @FullPath + ''', SINGLE_BLOB) AS img
            )
            WHERE Id = ' + CAST(@Id AS nvarchar(10)) + ';';

        BEGIN TRY
            EXEC sp_executesql @sql;
        END TRY
        BEGIN CATCH
            PRINT 'Failed to load ' + ISNULL(@ImageUrl, '<null>') + ': ' + ERROR_MESSAGE();
            -- continue to next file
        END CATCH

        FETCH NEXT FROM image_cursor INTO @Id, @ImageUrl;
    END

    CLOSE image_cursor;
    DEALLOCATE image_cursor;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    PRINT 'Error during image load: ' + ERROR_MESSAGE();
    ROLLBACK TRANSACTION;
END CATCH
GO

-- If OPENROWSET with dynamic path is not allowed in your environment, uncomment and use static explicit updates:
-- UPDATE Products
-- SET image = (SELECT BulkColumn FROM OPENROWSET(BULK N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\product1.png', SINGLE_BLOB) AS img)
-- WHERE ImageUrl = 'product1.png';

-- Show results
select Id, name, ImageUrl, DATALENGTH([image]) as ImageBytes from Products;
GO
