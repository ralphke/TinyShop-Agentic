# Part 7: Debugging with Copilot

In this section you'll use Copilot to diagnose runtime exceptions and analyse variables — without leaving VS Code.

## Debug an exception

1. Start the app with `aspire run` and open the Store in your browser.
1. Navigate to the **About** page in the navigation menu.
1. Observe that an exception occurs and the page crashes.
1. Switch back to VS Code and open the **Problems** panel or the integrated terminal where `aspire run` is running — you should see a stack trace.
1. Copy the exception message and stack trace, then paste it into Copilot Chat:
   ```
   The About page throws this exception when I navigate to it:
   <paste stack trace here>
   What is the cause and how do I fix it?
   ```
1. Review how Copilot identifies the root cause and suggests a fix.
1. Apply the fix and restart the app to verify the page loads correctly.

## Set a breakpoint and inspect variables

1. Open **Store/Components/Pages/Products.razor**.
1. Add a breakpoint at the end of the `OnInitializedAsync` method by clicking in the gutter next to that line.
1. Start the app in debug mode: press **F5** or use **Run → Start Debugging** in VS Code (with the C# DevKit extension installed).
1. Navigate to the Products page in the browser to hit the breakpoint.
1. In the **Variables** panel (or by hovering), inspect the `products` list variable.
1. In Copilot Chat, ask:
   ```
   I'm debugging Products.razor. The products list has these values: <paste variable state>. Are there any products with "outdoor" in the name that cost under $40?
   ```
1. Observe how Copilot can reason about variable state and suggest equivalent LINQ queries.

**Key Takeaway**: Copilot can accelerate debugging by explaining stack traces, suggesting fixes, and helping you reason about variable state — all within your editor.

