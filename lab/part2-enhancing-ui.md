# Part 2: Enhancing UI with Inline Chat

In this section you'll use Copilot's inline chat to improve the UI without leaving your editor context.

> **NOTE:** The prompts below are starting points. Feel free to rephrase them in natural language — describing *what* you want rather than *how* to implement it often produces the best results.

## Add a loading spinner

1. Open **Store/Components/Pages/Products.razor**:
   - **VS Code / Codespaces**: use the **Explorer** panel
   - **Visual Studio 2026**: use the **Solution Explorer** under the **Store** project → **Components/Pages**

1. Find the `"Loading..."` text in the component markup.

1. Select that text, then open the inline chat:

   | IDE | How to open inline chat |
   |:----|:------------------------|
   | **VS Code / Codespaces** | Press `Ctrl+I` (Windows/Linux) or `Cmd+I` (Mac) |
   | **Visual Studio 2026** | Right-click the selection and choose **Ask Copilot**, or press `Alt+/` |

1. Type: `Update this to show a Bootstrap loading spinner`

1. Review the suggestion and press **Accept** (or **Tab**) if it looks correct.

   The result should look similar to:
   ```html
   <div class="spinner-border text-primary" role="status">
       <span class="visually-hidden">Loading...</span>
   </div>
   ```

1. Start the app and navigate to the **Products** page to see the spinner in action:
   - **VS Code / Codespaces**: `aspire run` in the terminal
   - **Visual Studio 2026**: Press **F5**

1. Stop the app when done.

**Key Takeaway**: Inline chat lets you make targeted improvements to specific code sections without switching context, and without opening a separate chat window.


