-- Recreate vector column with matching dimensions
ALTER TABLE dbo.Products DROP COLUMN DescriptionVector;
ALTER TABLE dbo.Products ADD DescriptionVector VECTOR(768, FLOAT32) NULL;

-- Refill from JSON string column
UPDATE dbo.Products
SET DescriptionVector = CAST(DescriptionEmbedding AS VECTOR(768, FLOAT32))
WHERE DescriptionEmbedding IS NOT NULL;