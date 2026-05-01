# Part 2: Enhancing UI with Inline Chat

In this section you'll use Copilot's inline chat to add new capabilities to the Store UI without leaving your editor context.

> **NOTE:** The prompts below are starting points. Feel free to rephrase them in natural language — describing *what* you want rather than *how* to implement it often produces the best results.

## Exercise 1 — Add a live product search bar

The Products API now has a `/search` endpoint (added in Part 1). Wire it up in the UI so users can filter the product list in real time.

1. Open **Store/Components/Pages/Products.razor**:
   - **VS Code / Codespaces**: use the **Explorer** panel
   - **Visual Studio 2026**: use the **Solution Explorer** under the **Store** project → **Components/Pages**

1. Place your cursor just above the `products-grid` div, then open the inline chat:

   | IDE | How to open inline chat |
   |:----|:------------------------|
   | **VS Code / Codespaces** | Press `Ctrl+I` (Windows/Linux) or `Cmd+I` (Mac) |
   | **Visual Studio 2026** | Right-click the selection and choose **Ask Copilot**, or press `Alt+/` |

1. Type:
   ```
   Add a search input above the product grid. When the user types, call ProductService.SearchProductsAsync(query) and update the products list. Debounce the call by 300 ms.
   ```

1. Review the suggestion, accept it, and make sure `SearchProductsAsync` is referenced correctly in the `@code` block.

1. Start the app and verify the search bar filters the product list as you type:
   - **VS Code / Codespaces**: `aspire run` in the terminal
   - **Visual Studio 2026**: Press **F5**

1. Stop the app when done.

## Exercise 2 — Add sort controls with inline chat

1. In **Products.razor**, select the `<div class="mb-3 text-muted">` block that shows the product count.

1. Open the inline chat and type:
   ```
   Add a sort dropdown next to the product count that lets the user sort by Name (A→Z), Price (low→high), and Price (high→low). Apply the sort to the products list in the @code block.
   ```

1. Accept the suggestion and run the app to verify the sort options work.

**Key Takeaway**: Inline chat lets you describe *behaviour* in plain English and Copilot generates the wiring — both the markup and the `@code` logic — in one step, without switching context.

