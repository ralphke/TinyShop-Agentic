# Part 1: Ghost Text, NES, and Copilot Completions

In this section you'll use GitHub Copilot's inline code completion (Ghost Text) and Next Edit Suggestions (NES) to extend the TinyShop API with a **keyword search** feature that doesn't exist yet — experiencing how Copilot accelerates real feature work rather than just boilerplate generation.

> **IMPORTANT:** Type the code manually rather than copying and pasting snippets. This lets you experience Ghost Text and NES as you would in your daily work — you'll often only need to type a few characters before Copilot completes the rest.

## Context: what's already there

The Products API (`/api/Product`) currently supports full CRUD, image upload, and a debug endpoint. The `AgentGateway` has semantic and keyword search — but the **main Product API has no plain text search endpoint**. We'll add one now.

## Exercise 1 — Add a keyword search endpoint with Ghost Text

| IDE | How to open the file |
|:----|:---------------------|
| **VS Code / Codespaces** | Explorer panel → **Products/Endpoints/ProductEndpoints.cs** |
| **Visual Studio 2026** | Solution Explorer → **Products** project → **Endpoints/ProductEndpoints.cs** |

1. Inside `MapProductEndpoints`, place your cursor on an empty line **after the `/count` endpoint block** and start typing:

   ```csharp
   // GET products by keyword
   group.MapGet("/search
   ```

1. Wait for the Ghost Text suggestion (gray text). Press **Tab** to accept it, or keep typing to steer the suggestion.

1. The completed endpoint should filter products by a `query` string parameter against both the `Name` and `Description` columns:

   ```csharp
   // GET products by keyword
   group.MapGet("/search", async (string? query, ProductDataContext db) =>
   {
       if (string.IsNullOrWhiteSpace(query))
           return Results.BadRequest("query parameter is required");

       var results = await db.Product
           .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
           .ToListAsync();

       return Results.Ok(results);
   })
   .WithName("SearchProducts")
   .Produces<List<Product>>(StatusCodes.Status200OK)
   .Produces(StatusCodes.Status400BadRequest);
   ```

   > The exact wording Copilot suggests may differ — any working implementation is fine.

## Exercise 2 — Extend ProductService with Ghost Text

Now add a matching search method to the Store's `ProductService` so the frontend can call the new endpoint.

| IDE | How to open the file |
|:----|:---------------------|
| **VS Code / Codespaces** | Explorer panel → **Store/Services/ProductService.cs** |
| **Visual Studio 2026** | Solution Explorer → **Store** project → **Services/ProductService.cs** |

1. Place your cursor on an empty line **after `GetProductByIdAsync`** and start typing:

   ```csharp
   public async Task<List<Product>> Search
   ```

1. Accept Ghost Text to complete the method. It should call the `/api/Product/search?query=…` endpoint and return a `List<Product>`.

   > Copilot has full context of the existing methods (`GetProducts`, `GetProductByIdAsync`) and will match their patterns and caching style.

## Exercise 3 — Try Next Edit Suggestions (NES)

NES automatically highlights other locations that need updating when you make a change.

1. In **ProductEndpoints.cs**, rename the private helper at the bottom of the file:
   ```
   BuildEmbeddingText   →   BuildProductEmbeddingText
   ```

1. After you rename the method declaration, notice how NES highlights the **two call sites** inside `MapPost` and `MapPut` and offers to update them automatically — press **Tab** to accept each suggestion.

## Exercise 4 — Generate XML documentation

1. Place your cursor on the blank line **above the `MapProductEndpoints` method signature**.
1. Type `///` — Copilot suggests a complete XML doc comment. Accept it with **Tab**.
1. Repeat for the new private `BuildProductEmbeddingText` helper you just renamed.

## Test your changes

1. Run the app:
   - **VS Code / Codespaces**: `aspire run` in the terminal
   - **Visual Studio 2026**: Press **F5**
1. From the Aspire Dashboard, open the Products API URL and navigate to:
   ```
   /api/Product/search?query=outdoor
   ```
   Verify your new endpoint returns matching products.
1. Stop the app when done.

**Key Takeaway**: Ghost Text turns a blank line into working code in seconds, while Next Edit Suggestions ensure your rename propagates consistently — both dramatically reduce the mechanical overhead of extending an existing codebase.
