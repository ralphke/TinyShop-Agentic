# TinyShop - Setup Guide

## Overview

TinyShop is a .NET 10 e-commerce sample with:

- Products API (Minimal API + EF Core + SQL Server)
- Store (Blazor Server frontend)
- Aspire AppHost orchestration
- Docker Compose workflow for local containerized runs

## Current Features

- Product catalog with database-backed images
- Shopping cart flow (add items, cart badge, clear cart)
- Checkout flow and order confirmation page
- Impressum page for RaKeTe-Technology in navigation

## Run Options

### Option A: Aspire (recommended for solution development)

```bash
dotnet run --project TinyShop.AppHost
```

### Option B: Docker Compose

From repository root:

```bash
docker compose up -d --build
```

Endpoints in compose mode:

- Store: http://localhost:5158
- Products API: http://localhost:5228/api/Product
- Checkout: http://localhost:5158/checkout
- Impressum: http://localhost:5158/impressum

Stop compose:

```bash
docker compose down
```

## Image Loading and Seeding

On first startup, Products initializes the database and seeds product data.
The initializaion process uses also the huggingface/textembedings LLM to create the embeddings for Product name and description
If image bytes are missing in an existing DB, use:

```powershell
cd D:\repros\VS2022-lab300\src\Products\SQL
.\LoadImages.ps1
```

## Configuration Notes

- Store uses ProductService as typed HttpClient and reads ProductEndpoint from config.
- Products uses SQL Server by default.
- Products uses EF Core InMemory only when environment is Testing (for integration tests).
- HTTPS redirection is configurable through EnableHttpsRedirection (used as false in compose).

## Tests

Run all test projects:

```bash
dotnet test src/Store.Tests/Store.Tests.csproj
dotnet test tests/Store.UnitTests/Store.UnitTests.csproj
dotnet test tests/Store.IntegrationTests/Store.IntegrationTests.csproj
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
dotnet test src/Tests/TinyShopTest/TinyShopTest.csproj
```

## Troubleshooting

### Products assembly blocked by local Windows Application Control

If you see a FileLoadException mentioning policy block on Products.dll, run via Docker Compose or dev container instead of direct host execution.

### Images not displaying

Use the debug endpoint:

- /api/Product/debug/images

Then reload images with LoadImages.ps1 if needed.
