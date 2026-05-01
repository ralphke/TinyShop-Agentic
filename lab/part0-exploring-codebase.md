# Part 0: Exploring the Codebase with GitHub Copilot Chat

GitHub Copilot Chat lets you ask questions about your code and receive intelligent, context-aware answers.

## Open Copilot Chat

1. In VS Code, press `Ctrl+Alt+I` (Windows/Linux) or `Cmd+Ctrl+I` (Mac) to open the **GitHub Copilot Chat** panel, or click the Copilot icon in the Activity Bar.

## Ask questions about the project

1. Try asking about the overall structure:
   - `What projects are in this solution and how do they work together?`
   - `How does the Products API work?`
   - `What does the AgentGateway project do?`

1. Ask more specific questions:
   - `How is the shopping cart implemented?`
   - `Where is the checkout flow defined?`
   - `What MCP tools does TinyShopMcpTools expose?`

1. Notice how Copilot reads the actual source files in your workspace to give contextual, accurate answers.

## Use `@workspace` for broader searches

1. Prefix a question with `@workspace` to instruct Copilot to search across all files:
   - `@workspace Where is the Product model defined?`
   - `@workspace How are images stored and served?`
   - `@workspace List all API endpoints in the Products project`

**Key Takeaway**: GitHub Copilot Chat helps you understand unfamiliar codebases quickly by answering questions about project structure, architecture, and implementation details using your actual source code as context.

