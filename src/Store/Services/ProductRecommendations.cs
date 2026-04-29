using DataEntities;

namespace Store.Services;

public static class ProductRecommendations
{
    private static readonly Dictionary<int, int[]> RecommendedProductsById = new()
    {
        [1] = [4, 8, 2],
        [2] = [3, 5, 9],
        [3] = [2, 5, 9],
        [4] = [1, 8, 5],
        [5] = [2, 3, 6],
        [6] = [7, 8, 9],
        [7] = [6, 8, 9],
        [8] = [1, 7, 9],
        [9] = [8, 5, 6]
    };

    public static List<Product> GetFor(Product product, IReadOnlyCollection<Product> allProducts)
    {
        if (!RecommendedProductsById.TryGetValue(product.Id, out var relatedIds))
        {
            return [];
        }

        return allProducts
            .Where(candidate => relatedIds.Contains(candidate.Id) && candidate.Id != product.Id)
            .OrderBy(candidate => Array.IndexOf(relatedIds, candidate.Id))
            .ToList();
    }
}