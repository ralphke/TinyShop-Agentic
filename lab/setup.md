# Workshop Setup

## Environment options

You have two ways to run this lab — pick the one that suits you best.

### Option A — GitHub Codespaces (recommended)

No local tools required. Everything runs in the cloud.

1. Open [https://github.com/ralphke/TinyShop-Agentic](https://github.com/ralphke/TinyShop-Agentic).
2. Click **Code → Codespaces → Create codespace on main**.
3. Wait for the container to build (~2 minutes). VS Code opens in your browser.
4. In the integrated terminal, run:
   ```bash
   aspire run
   ```
5. The Aspire Dashboard tab opens automatically. Use the links there to navigate to the **Store** and **Products API**.

### Option B — Local Dev Container (VS Code + Docker Desktop)

1. Install [VS Code](https://code.visualstudio.com/) and the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension.
2. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) and start it.
3. Clone the repository:
   ```bash
   git clone https://github.com/ralphke/TinyShop-Agentic.git
   cd TinyShop-Agentic
   ```
   > **Windows users:** Clone inside your WSL filesystem (e.g. `~/src/TinyShop-Agentic`), not under `/mnt/d/...`.
4. Open the folder in VS Code.
5. When prompted, click **Reopen in Container** (or run **Dev Containers: Reopen in Container** from the Command Palette).
6. Once provisioning finishes, start the app:
   ```bash
   aspire run
   ```

## Configure GitHub Copilot

1. In VS Code, open the **Extensions** panel (`Ctrl+Shift+X` / `Cmd+Shift+X`).
2. Search for **GitHub Copilot** and ensure the extension is installed and enabled.
3. Sign in with your GitHub account when prompted. A Free-tier Copilot subscription is sufficient.
4. Verify that the Copilot status icon in the bottom-right of VS Code is active (not grayed out).

## Explore the running application

Once `aspire run` starts all services, the following ports are forwarded:

| Service         | URL                                  |
|:----------------|:-------------------------------------|
| Aspire Dashboard | http://localhost:15218              |
| Store (Blazor)  | http://localhost:5158                |
| Products API    | http://localhost:5228/api/Product    |
| Checkout        | http://localhost:5158/checkout       |
| Agent Gateway   | http://localhost:5290/api/agent-gateway/agent-card |

Open the Store in your browser, browse products, and add a few items to the cart.

## Project structure at a glance

```
TinyShop.AppHost/   → Aspire orchestrator
Products/           → ASP.NET Core Minimal API (backend)
Store/              → Blazor Server frontend
AgentGateway/       → A2A-compatible REST adapter + MCP server
DataEntities/       → Shared Product model
Store.Tests/        → Unit & component tests
```

You're all set — proceed to [Part 0: Exploring the Codebase](part0-exploring-codebase.md).

1. [] Stop debugging and close the application.

## Summary and next steps

You've now cloned the repository you'll use for this for the rest of the workshop.
