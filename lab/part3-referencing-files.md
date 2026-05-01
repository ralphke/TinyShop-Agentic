# Part 3: Referencing Code Files in Chat

In this section you'll learn how to attach files and symbols to a Copilot Chat conversation to get more accurate, project-specific suggestions.

## Reference a file with `#file`

1. Open the Copilot Chat panel (`Ctrl+Alt+I` / `Cmd+Ctrl+I`).
1. Start a new chat by clicking the **+** icon at the top of the panel.
1. Make sure the mode is set to **Ask** (not Agent).
1. Type the following prompt, using `#file` to attach the service:

   ```
   #file:ProductService.cs How would I implement displaying products in a table, including the CSS needed?
   ```

1. Review the code suggestion but **do not implement it yet**.
1. Notice how referencing the file gives Copilot the context of the actual method signatures, return types, and caching logic already in place.

## Reference a symbol with `#sym`

1. Start another new chat.
1. Try referencing a specific symbol:

   ```
   #sym:CartService How does the cart handle the OnChange event and why is it important for Blazor Server?
   ```

1. Observe how Copilot answers based on the real implementation rather than guessing.

**Key Takeaway**: Attaching files and symbols with `#file` and `#sym` provides Copilot with the exact context it needs to give accurate, project-specific suggestions instead of generic examples.

