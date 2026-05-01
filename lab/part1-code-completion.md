# Part 1: Code Completion with Ghost Text

In this section you'll use GitHub Copilot's inline code completion (Ghost Text) to implement or extend API endpoints.

> **IMPORTANT:** Type the code manually rather than copying and pasting snippets. This lets you experience Ghost Text suggestions as you would in your daily work — you'll often only need to type a few characters before Copilot completes the rest.

## Add a new endpoint

1. In the Explorer panel, open **Products/Endpoints/ProductEndpoints.cs**.

1. Inside the `MapProductEndpoints` method, place your cursor on an empty line after the existing endpoints and start typing:
   ```csharp
   g
   ```

1. Wait for the Ghost Text suggestion to appear (shown in gray). Press **Tab** to accept it, or keep typing to refine the suggestion.

1. Use Ghost Text to complete the full set of CRUD endpoints:
   - `GET /{id}` — get a product by ID
   - `POST /` — create a product
   - `PUT /{id}` — update a product
   - `DELETE /{id}` — remove a product

   The completed endpoints should look similar to:

   ```csharp
   group.MapGet("/{id:int}", async (int id, ProductDataContext db) =>
   {
       return await db.Product.AsNoTracking()
           .FirstOrDefaultAsync(model => model.Id == id)
           is Product model
               ? Results.Ok(model)
               : Results.NotFound();
   })
   .WithName("GetProductById")
   .Produces<Product>(StatusCodes.Status200OK)
   .Produces(StatusCodes.Status404NotFound);
   ```

   > Because LLMs are probabilistic, the exact suggestion may differ. Any working implementation is fine.

## Try Next Edit Suggestions (NES)

1. In the `GET /{id}` endpoint, rename the parameter from `id` to `productId`.
1. Notice how Copilot's Next Edit Suggestions highlight the other places in the same scope that also need updating, and offer to apply them automatically.

## Generate XML documentation

1. Place your cursor above the `MapProductEndpoints` method.
1. Type `///` — Copilot will suggest a complete XML doc comment. Accept it with **Tab**.

## Test your changes

1. Run the app: `aspire run`
1. Open the Products API URL from the Aspire Dashboard and navigate to `/api/Product/1` to verify your new endpoint.
1. Stop the app when done.

**Key Takeaway**: Ghost Text and Next Edit Suggestions let you implement complete API methods from minimal keystrokes, significantly accelerating development.

