<p align="center">
<img src="img/banner.jpg" alt="decorative banner" width="1200"/>
</p>

# LAB300 - Hands-on with GitHub Copilot in Visual Studio 2022

This lab will guide you through using GitHub Copilot's various features in Visual Studio 2022. You'll start with a partially completed TinyShop application and use GitHub Copilot to complete missing features and enhance the application.

## Prerequisites

- Visual Studio 2022 with GitHub Copilot extension installed
- starting Visual Studio 2022 >= 17.13, GitHub Copilot is integrated with the VS Shell
- .NET 10 SDK
- GitHub account with Copilot subscription (including Free)
- make sure your nuget packages match the requiements by running the following commnads in the T.\src folder
- dotnet nuget locals all --clear
- dotnet restore
- This will make sure that your donet environment matches the project settings
- Next you need your browser to trust the development certificates by executing the following command
- dotnet dev-certs https --trust
- now all should be setup to work as expected on your computer
- To clean-up the environment, you can run the following command
- dotnet dev-certs https --clean
- This will remove all developer certificates from your machine

## Current Workspace Status

The workspace currently includes the following completed updates:

- Shopping cart and checkout flow restored in the Store frontend
- Order confirmation page added
- Impressum page for RaKeTe-Technology added and linked from the navigation
- Test projects aligned to .NET 10
- Docker Compose setup hardened for HTTP-only local runtime and persistent DataProtection keys

## Run with Docker Compose

The Docker Compose stack uses the SQL Server connection string from `.env`.
Make sure your `.env` includes the connection string and SA password, for example:

```env
PRODUCTS_DB_CONNECTION_STRING=Server=sqlserver,1433;Database=TinyShopDB;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;
MSSQL_SA_PASSWORD=your-sa-password-here

```
```bash
docker-compose up 
```


> Note: the `products` app no longer performs EF Core database creation or seeding at startup. The schema must exist before the service starts.


Useful endpoints:

- Store: http://localhost:5158
- Products API: http://localhost:5228/api/Product
- Checkout: http://localhost:5158/checkout
- Impressum: http://localhost:5158/impressum

Stop the stack:

```bash
docker-compose down
```

Quick health check:

```powershell
curl.exe -s -o NUL -w "products:%{http_code}`n" http://localhost:5228/api/Product
curl.exe -s -o NUL -w "store:%{http_code}`n" http://localhost:5158/
curl.exe -s -o NUL -w "checkout:%{http_code}`n" http://localhost:5158/checkout
curl.exe -s -o NUL -w "impressum:%{http_code}`n" http://localhost:5158/impressum
```

## Run Tests

Run all test projects explicitly:

```bash
dotnet test src/Store.Tests/Store.Tests.csproj
dotnet test tests/Store.UnitTests/Store.UnitTests.csproj
dotnet test tests/Store.IntegrationTests/Store.IntegrationTests.csproj
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
dotnet test src/Tests/TinyShopTest/TinyShopTest.csproj
```

## WSL Dev Container

This repository includes a Linux dev container in [.devcontainer/devcontainer.json](.devcontainer/devcontainer.json) with Docker-in-Docker enabled so Aspire can start its SQL Server container from inside the development container.

Use this workflow on Windows:

1. Install WSL 2 and a Linux distro such as Ubuntu.
2. Install Docker Desktop and enable WSL integration for that distro.
3. Clone the repository inside the WSL filesystem, for example under `~/src/VS2022-lab300`, instead of working from `/mnt/d/...`.
4. Open the folder from VS Code in a WSL window.
5. Run `Dev Containers: Reopen in Container`.
6. After the container finishes provisioning, start the app with `aspire run`.

Quick verification inside WSL before reopening in the container:

```bash
docker version
```

If that command fails in WSL, the dev container will not be able to provision Docker support correctly.


## Lab Overview

The TinyShop application consists of two main projects:
- A backend API built with .NET Minimal APIs
- A frontend Blazor Server application

You'll use GitHub Copilot's various features to enhance and complete this application.

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

**Key Takeaway**: These tools can significantly boost your productivity as a developer by automating repetitive tasks, generating boilerplate code, and helping you implement complex features more quickly.

## Session Resources 

| Resources          | Links                             | Description        |
|:-------------------|:----------------------------------|:-------------------|
| Build session page | https://build.microsoft.com/sessions/LAB300 | Event session page with downloadable recording, slides, resources, and speaker bio |
|Microsoft Learn|https://aka.ms/AAI_DevAppGitHubCop_Plan|Official Collection or Plan with skilling resources to learn at your own pace|
