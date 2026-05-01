# Workshop Setup

## Environment options

You have three ways to run this lab — pick the one that suits you best.

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

### Option C — Visual Studio 2026

1. Install [Visual Studio 2026](https://visualstudio.microsoft.com/) with the **ASP.NET and web development** workload and the **.NET Aspire** workload component.
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) if it is not bundled with your VS installer.
3. Clone the repository and open **src/TinyShop.sln** (or **src/TinyShop.AppHost/TinyShop.AppHost.sln** if you want the AppHost-focused solution) in Visual Studio.
4. Set **TinyShop.AppHost** as the startup project, then press **F5** (or **Debug → Start Debugging**).

   The .NET Aspire AppHost starts all services and opens the Aspire Dashboard automatically.

## Configure GitHub Copilot

### VS Code / Codespaces

1. Open the **Extensions** panel (`Ctrl+Shift+X` / `Cmd+Shift+X`).
2. Search for **GitHub Copilot** and ensure the extension is installed and enabled.
3. Sign in with your GitHub account when prompted.
4. Verify that the Copilot status icon in the bottom-right is active (not grayed out).

### Visual Studio 2026

GitHub Copilot is integrated into the VS 2026 shell — no separate extension install is needed.

1. Click the **Copilot icon** in the top bar (left side, next to the search box).
2. Click **Sign in to use Copilot**.
3. A browser window opens; sign in with your GitHub account and click **Authorize Visual Studio**.
4. When the browser asks to open Visual Studio, click **Open**.
5. The Copilot icon turns green when sign-in succeeds.

## Explore the running application

Once the app is running, the following ports are forwarded (Aspire) or available (Docker Compose):

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

