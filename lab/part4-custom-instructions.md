# Part 4: Using Custom Instructions

Copilot instructions let you encode project-wide context — the technologies in use, coding conventions, test placement, etc. — so every chat response automatically follows your standards.

## Why custom instructions matter

Without instructions, Copilot may suggest:
- `AddSingleton` instead of `AddScoped` for a service
- `Assert.Equal` instead of FluentAssertions `.Should().Be()`
- Inline `<style>` blocks instead of `.razor.css` files

A well-crafted instructions file eliminates these mismatches for every developer on the team.

## Review the existing instructions file

1. In the Explorer panel, open **.github/copilot-instructions.md**.
1. Read the existing content — it describes the architecture, dev container, running, testing, and coding conventions.

## Extend the instructions

1. Open Copilot Chat and ask: `@workspace What coding conventions are NOT yet captured in .github/copilot-instructions.md?`
1. Add any missing conventions you'd like Copilot to follow. Some suggestions:
   - Preferred error handling patterns
   - Naming conventions for Blazor components
   - Rules for adding new Aspire resources

## Verify the instructions take effect

1. Start a **new** chat (click **+**) so the updated file is included.
1. Re-run the prompt from Part 3:
   ```
   #file:ProductService.cs How would I implement getting and visualizing the products in a table, including the CSS?
   ```
1. Compare the response to what you received before. Notice how the suggestions now align with the conventions in the instructions file (e.g., `.razor.css` files, FluentAssertions syntax, correct service lifetimes).

**Key Takeaway**: Custom instructions make Copilot's suggestions consistently aligned with your project's standards and architecture, benefiting every developer who works in the repository.

