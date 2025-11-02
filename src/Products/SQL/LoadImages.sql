-- Helper Script: Load Images from File System into Database
-- This script demonstrates how to load PNG images from disk into the ImageData column
-- NOTE: This requires OPENROWSET which needs special permissions and may not work in all environments
-- For production, use the API endpoint PUT /api/Product/{id}/image instead

USE TestDB;
GO

-- Enable advanced options (requires sysadmin or appropriate permissions)
-- EXEC sp_configure 'show advanced options', 1;
-- RECONFIGURE;
-- EXEC sp_configure 'Ad Hoc Distributed Queries', 1;
-- RECONFIGURE;
-- GO

-- Loop through products 1-9 and load their corresponding images
DECLARE @ProductId INT = 1;
DECLARE @MaxProductId INT = 9;
DECLARE @ImagePath NVARCHAR(500);
DECLARE @SQL NVARCHAR(MAX);

WHILE @ProductId <= @MaxProductId
BEGIN
    -- Construct the image file path
    SET @ImagePath = N'D:\repros\VS2022-lab300\src\Products\wwwroot\images\product' + CAST(@ProductId AS NVARCHAR(10)) + N'.png';
    
    -- Build dynamic SQL to load the image
    SET @SQL = N'UPDATE dbo.Products 
            SET ImageData = (SELECT * FROM OPENROWSET(BULK ''' + @ImagePath + N''', SINGLE_BLOB) AS ImageData)
     WHERE Id = ' + CAST(@ProductId AS NVARCHAR(10)) + N';';
    
    -- Execute the dynamic SQL
  BEGIN TRY
        EXEC sp_executesql @SQL;
        PRINT 'Successfully loaded image for Product ID: ' + CAST(@ProductId AS NVARCHAR(10));
    END TRY
    BEGIN CATCH
      PRINT 'Error loading image for Product ID: ' + CAST(@ProductId AS NVARCHAR(10)) + ' - ' + ERROR_MESSAGE();
  END CATCH
    
    -- Move to next product
    SET @ProductId = @ProductId + 1;
END
GO

-- Alternatively, use the PowerShell script or API endpoint for loading images
PRINT '';
PRINT 'To load images, use one of the following methods:';
PRINT '1. PowerShell script: Products/SQL/LoadImages.ps1';
PRINT '2. API endpoint: PUT /api/Product/{id}/image';
PRINT '3. OPENROWSET (requires special permissions - see comments in this file)';
GO
