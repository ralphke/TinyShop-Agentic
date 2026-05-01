# Part 6: Using Copilot Vision

Copilot Vision lets you attach images to a chat message — share a screenshot of a bug, a UI mockup, a design comp, or a diagram, and Copilot will interpret it and generate appropriate code.

## Redesign the product grid from an image

1. Open Copilot Chat and start a new **Agent** mode conversation.
1. Click the **paperclip / attach** icon in the chat input box and select an image. Use the **eshop.png** file in the root of the repository as a design reference.
1. Ask:
   ```
   Update Products.razor to display products in a card grid layout similar to this image. Add hover effects, keep it responsive, and put all styles in Products.razor.css.
   ```
1. Review the proposed changes to **Products.razor** and **Products.razor.css**.
1. Accept the changes and run the app (`aspire run`) to see the result.
1. If the layout isn't quite right, iterate:
   ```
   The card images are too tall. Constrain them to 200px height with object-fit: cover.
   ```

> **Note:** If you don't see the updated CSS in the browser, do a hard refresh with `Ctrl+Shift+R` / `Cmd+Shift+R`.

## Debug from a screenshot

1. Take a screenshot of any visual issue you notice in the running app (or create one deliberately, e.g. by removing a CSS class).
1. Attach the screenshot to a new Copilot Chat message and describe the problem:
   ```
   The product images in this screenshot are stretched. How do I fix this in the CSS?
   ```
1. Review Copilot's diagnosis and suggested fix.

**Key Takeaway**: Copilot Vision bridges the gap between design and code — attach any image and Copilot will reason about it alongside your source files.

