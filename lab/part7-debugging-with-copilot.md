# Part 7: Debugging with Copilot

In this section you'll use Copilot to diagnose runtime exceptions and analyse variables — without leaving your IDE.

## Set a breakpoint and inspect variables

1. Open **Store/Components/Pages/Products.razor**.
1. Add a breakpoint at the end of the `OnInitializedAsync` method by clicking in the gutter next to that line.
1. Start the app in debug mode:
   - **VS Code / Codespaces**: Press **F5** (requires C# DevKit extension)
   - **Visual Studio 2026**: Press **F5**

1. Navigate to the Products page in the browser to hit the breakpoint.

1. Inspect the `products` list variable:

   | IDE | How to inspect |
   |:----|:---------------|
   | **VS Code / Codespaces** | Hover over the variable in the **Variables** panel; paste values into Copilot Chat |
   | **Visual Studio 2026** | Hover over `products`, click the **magnifier (View)** button, then click the **sparkle ✨** button and type a natural-language filter: `Products with "outdoor" in the name that cost under $40` |

1. In Copilot Chat, ask:
   ```
   I'm debugging Products.razor. The products list has these values: <paste variable state>. Are there any products with "outdoor" in the name that cost under $40?
   ```
1. Observe how Copilot can reason about variable state and suggest equivalent LINQ queries.

> **Visual Studio 2026 tip:** After using the sparkle filter in the visualizer, click **Continue in Chat** to have Copilot generate the full LINQ expression for the same criteria.

**Key Takeaway**: Copilot can accelerate debugging by explaining stack traces, suggesting fixes, and helping you reason about variable state — all within your IDE.


