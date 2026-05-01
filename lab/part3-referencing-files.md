# Part 3: Referencing Code Files in Chat

In this section you'll learn how to attach files and symbols to a Copilot Chat conversation to get more accurate, project-specific suggestions.

## Open a new chat in Ask mode

| IDE | Steps |
|:----|:------|
| **VS Code / Codespaces** | Open Copilot Chat (`Ctrl+Alt+I` / `Cmd+Ctrl+I`) → click **+** → set mode to **Ask** |
| **Visual Studio 2026** | Open Copilot Chat (`Ctrl+\, Ctrl+C`) → click the **+** new-chat icon → set mode to **Ask** |

## Reference a file with `#file`

1. In the chat input, type the following prompt using `#file` to attach the service:

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

> **Visual Studio 2026 tip:** You can also drag a file from the **Solution Explorer** directly into the Copilot Chat input box to attach it as context, in addition to using `#file`.

**Key Takeaway**: Attaching files and symbols with `#file` and `#sym` provides Copilot with the exact context it needs to give accurate, project-specific suggestions instead of generic examples.


