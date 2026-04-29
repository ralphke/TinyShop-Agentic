-- This code isn't working with the current SQL Server 2025 CU3 Bulid.

CREATE OR ALTER PROCEDURE dbo.usp_HybridProductSearch
    @QueryEmbeddings NVARCHAR(MAX),
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @QueryVector VECTOR(768) =  CAST(@QueryEmbeddings AS VECTOR(768));
    SELECT TOP (@TopN) --WITH APPROXIMATE
        p.Id,
        p.Name,
        p.Description,
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
    ORDER BY vs.distance ASC;
END
GO
