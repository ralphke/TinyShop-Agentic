# Part 5: Implementing Features with Copilot Agent

Copilot Agent mode (also called **Agentic** mode) can make coordinated changes across many files at once — it reads your codebase, plans the work, edits multiple files, and can run terminal commands with your permission.

## Switch to Agent mode

1. Open Copilot Chat (`Ctrl+Alt+I` / `Cmd+Ctrl+I`).
1. Click **+** to start a fresh conversation.
1. In the model/mode selector at the bottom of the panel, switch from **Ask** to **Agent**.

## Implement a product listing page

1. Type the following prompt:

   ```
   Implement a product listing page in Products.razor that fetches products from ProductService and displays them in a grid layout with product name, description, price, and image. Add hover effects and make it responsive.
   ```

   > Use your own phrasing. The key is providing a clear description of the desired outcome.

1. Watch as Copilot Agent proposes edits across **Products.razor**, **Products.razor.css**, and possibly other files.

## Review multi-file changes

1. Copilot lists every file it wants to change. Review each diff before accepting.
1. Click **Accept** on changes that look correct; click **Discard** for anything you want to redo.
1. If the result isn't quite right, continue the conversation:
   ```
   Make the grid use a minimum card width of 280px and add a subtle box shadow on hover.
   ```

   The markup should resemble:
   ```html
   @foreach (var product in products)
   {
       <div class="product-card">
           <img src="@ProductService.GetImageUrl(product)" alt="@product.Name" />
           <h3>@product.Name</h3>
           <p>@product.Description</p>
           <span>@product.Price.ToString("C")</span>
       </div>
   }
   ```

1. Run the app (`aspire run`) and verify the product grid renders correctly.
1. Stop the app when done.

## Let Agent implement a new feature end-to-end

1. Start a new Agent chat and try a larger prompt:
   ```
   Add a product search/filter bar to the Products page that filters the displayed products by name as the user types, without making additional API calls.
   ```

1. Review and accept the proposed changes.
1. Test the feature in the running app.

**Key Takeaway**: Copilot Agent mode can implement complete features spanning multiple files based on a natural-language description, compressing hours of boilerplate work into minutes.

