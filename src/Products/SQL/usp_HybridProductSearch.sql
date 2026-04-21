-- This code isn't working with the current SQL Server 2025 CU3 Bulid.

CREATE OR ALTER PROCEDURE dbo.usp_HybridProductSearch
    @QueryEmbeddings NVARCHAR(MAX),
    @CategoryFilter INT = NULL,
    @MinPrice DECIMAL(10,2) = NULL,
    @MaxPrice DECIMAL(10,2) = NULL,
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @QueryVector VECTOR(1536) =  CAST(@QueryEmbeddings AS VECTOR(1536));
    SELECT TOP (@TopN) --WITH APPROXIMATE
        p.Id,
        p.Name,
        p.Description,
        p.Category,
        p.Price,
        vs.distance AS SimilarityScore
    FROM VECTOR_SEARCH(
        TABLE = dbo.Products AS p,
        COLUMN = DescriptionVector,
        SIMILAR_TO = @QueryVector,
        METRIC = 'cosine',
        TOP_N = 100  -- Get more candidates for filtering
    ) AS vs
    WHERE p.DescriptionVector IS NOT NULL
        AND (@CategoryFilter IS NULL OR p.CategoryId = @CategoryFilter)
        AND (@MinPrice IS NULL OR p.Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR p.Price <= @MaxPrice)
    ORDER BY vs.distance ASC;
END
GO
