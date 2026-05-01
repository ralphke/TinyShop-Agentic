<p align="center">
<img src="img/banner.jpg" alt="decorative banner" width="1200"/>
</p>

# TinyShop-Agentic — Hands-on with GitHub Copilot Agentic Features

TinyShop-Agentic is a .NET Aspire cloud-native e-commerce sample that demonstrates GitHub Copilot's agentic capabilities. You'll explore the codebase, complete features, and extend the application using Copilot Chat, Agent mode, Vision, and custom instructions — from VS Code, GitHub Codespaces, or Visual Studio 2026.

## Prerequisites

Pick **one** of the following IDE environments:

- **GitHub Codespaces** — zero local install; runs entirely in the browser (recommended for workshops)
- **VS Code** with the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension + Docker Desktop
- **Visual Studio 2026** — GitHub Copilot is built into the shell; requires .NET 10 SDK

All options require a GitHub account with a Copilot subscription (Free tier is sufficient).

## Architecture

```
TinyShop.AppHost        → Aspire orchestrator; defines all resources and wiring
DataEntities            → Shared Product model (used by Products API and Store)
Products                → ASP.NET Core Minimal API; EF Core + SQL Server; port 5228
Store                   → Blazor Server (Interactive Server Components); port 5158
AgentGateway            → A2A-compatible REST adapter + MCP server for agent integration
TinyShop.ServiceDefaults→ Shared OpenTelemetry, health checks, resilience config
Store.Tests             → xUnit unit/component tests (FluentAssertions, bUnit, Moq)
Tests/IntegrationTests  → xUnit API + UI integration tests (WebApplicationFactory)
Tests/TinyShopTest      → MSTest basic tests
BenchmarkSuite1         → BenchmarkDotNet performance benchmarks
```

**Startup order** (enforced by Aspire): `products` starts first; `agent-gateway` and `store` start after `products` (potentially in parallel)

**Completed features in this workspace:**

- Shopping cart and checkout flow in the Store frontend
- Order confirmation page
- Impressum page for RaKeTe-Technology
- Agent Gateway service with A2A-compatible REST adapter and MCP server
- Test projects aligned to .NET 10
- Docker Compose setup for HTTP-only local runtime with persistent DataProtection keys

## Getting Started

### Option A — GitHub Codespaces (recommended, zero local setup)

1. Click **Code → Codespaces → Create codespace on main** on this repository.
2. Wait for the container to build (about 2 minutes).
3. In the VS Code terminal inside the Codespace, run:
   ```bash
   aspire run
   ```
4. The Aspire Dashboard opens automatically; use the forwarded-port links to reach the Store and Products API.

### Option B — Local Dev Container (VS Code + Docker Desktop)

This repository includes a Linux dev container in [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json) with Docker-in-Docker enabled to support Aspire and other container-based development workflows from inside the development container.

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop) and start it.
2. Clone the repository **inside your WSL filesystem** (e.g. `~/src/TinyShop-Agentic`) — do **not** clone under `/mnt/d/...`.
3. Open the folder in VS Code.
4. Run **Dev Containers: Reopen in Container** from the Command Palette.
5. After the container finishes provisioning, start the app:
   ```bash
   aspire run
   ```

Forwarded ports (HTTP):
- `15218` — Aspire Dashboard (opens automatically)
- `5228`  — Products API
- `5158`  — Store (Blazor)

### Run with Docker Compose (no Aspire)

From the repository root:

```bash
docker compose up -d --build
```

Useful endpoints:

- Store: http://localhost:5158
- Products API: http://localhost:5228/api/Product
- Checkout: http://localhost:5158/checkout
- Impressum: http://localhost:5158/impressum

Stop the stack:

```bash
docker compose down
```

## Run Tests

```bash
# Run all tests
dotnet test src/TinyShop.sln

# Run individual test projects
dotnet test src/Store.Tests/Store.Tests.csproj
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
dotnet test src/Tests/TinyShopTest/TinyShopTest.csproj
```

## Lab Parts

0. [Setup](lab/setup.md)
1. [Exploring the Codebase with GitHub Copilot Chat](lab/part0-exploring-codebase.md)
2. [Code Completion with Ghost Text](lab/part1-code-completion.md)
3. [Enhancing UI with Inline Chat](lab/part2-enhancing-ui.md)
4. [Referencing Code Files in Chat](lab/part3-referencing-files.md)
5. [Using Custom Instructions](lab/part4-custom-instructions.md)
6. [Implementing Features with Copilot Agent](lab/part5-implementing-features.md)
7. [Using Copilot Vision](lab/part6-copilot-vision.md)
8. [Debugging with Copilot](lab/part7-debugging-with-copilot.md)
9. [Commit Summary Descriptions](lab/part8-commit-summary-descriptions.md)

**Key Takeaway**: GitHub Copilot's agentic features can significantly boost your productivity by automating repetitive tasks, generating boilerplate code, and helping you implement complex features across multiple files.

## Session Resources

| Resources     | Links                                       | Description                                              |
|:--------------|:--------------------------------------------|:---------------------------------------------------------|
| Microsoft Learn | https://aka.ms/AAI_DevAppGitHubCop_Plan   | Official collection with skilling resources to learn at your own pace |
