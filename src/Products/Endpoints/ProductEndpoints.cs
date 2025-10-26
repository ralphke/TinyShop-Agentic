using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;

namespace Products.Endpoints;
/// <summary>   
/// Product API endpoints
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");
        // Count all products currently in store
        group.MapGet("/count", async (ProductDataContext db) =>
        {
            var count = await db.Product.CountAsync();
            return Results.Ok(count);
        });
        // GET all products
        group.MapGet("/", async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        // GET product by ID
        group.MapGet("/{productId:int}", async (int productId, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(productId);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST to create a new product
        group.MapPost("/", async (Product product, ProductDataContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Product/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        // PUT to update a product
        group.MapPut("/{productId:int}", async (int productId, Product updatedProduct, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(productId);
            if (product is null) return Results.NotFound();

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.ImageUrl = updatedProduct.ImageUrl;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateProduct")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE to remove a product
        group.MapDelete("/{productId:int}", async (int productId, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(productId);
            if (product is null) return Results.NotFound();

            db.Product.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteProduct")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
