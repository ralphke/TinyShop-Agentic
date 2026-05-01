# Part 2: Enhancing UI with Inline Chat

In this section you'll use Copilot's inline chat to improve the UI without leaving your editor context.

> **NOTE:** The prompts below are starting points. Feel free to rephrase them in natural language — describing *what* you want rather than *how* to implement it often produces the best results.

## Add a loading spinner

1. In the Explorer panel, open **Store/Components/Pages/Products.razor**.
1. Find the `"Loading..."` text in the component markup.
1. Select that text.
1. Press `Ctrl+I` (Windows/Linux) or `Cmd+I` (Mac) to open the **inline chat**.
1. Type: `Update this to show a Bootstrap loading spinner`
1. Review the suggestion and press **Accept** (or **Tab**) if it looks correct.

   The result should look similar to:
   ```html
   <div class="spinner-border text-primary" role="status">
       <span class="visually-hidden">Loading...</span>
   </div>
   ```

1. Start the app with `aspire run` and navigate to the **Products** page to see the spinner in action.
1. Stop the app when done.

**Key Takeaway**: Inline chat lets you make targeted improvements to specific code sections without switching context, and without opening a separate chat window.

