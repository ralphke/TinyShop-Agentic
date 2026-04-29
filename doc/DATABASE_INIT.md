# TinyShop Database Initialization

## Overview
The SQL Server database is auto-initialized on container startup using the `init-database.ps1` script and `Setup.sql`.

## Setup Instructions

### 1. Start Services
```bash
docker compose up -d
```

### 2. Initialize Database
After all services are running and SQL Server is healthy, run:
```bash
pwsh init-database.ps1
```

This script will:
- Wait for SQL Server to be ready
- Connect using credentials from `.env`
- Execute `Setup.sql` to create the TinyShopDB database
- Create all schema, tables, indexes, and initial product data

## What's Created
- **Database**: TinyShopDB
- **Tables**: Products, Customers, Orders, OrderItems, AgentCartSessions, AgentCartItems, AgentRequestAudits
- **Indexes**: Performance indexes on frequently queried columns
- **User**: TinyShopUser with db_datareader and db_datawriter roles
- **Initial Data**: Sample products and stored procedures for vector search

## Troubleshooting

### Script hangs on "Waiting for SQL Server to be ready..."
- Check that SQL Server container is running: `docker ps | grep sqlserver`
- Verify healthcheck: `docker inspect tinyshop-sqlserver | grep -A 5 Health`

### SSL Certificate Errors
The script uses the `-C` flag in sqlcmd to skip certificate verification for local development. This is safe in containerized environments.

### Database Already Exists
If TinyShopDB already exists and you want to reinitialize:
```bash
# Drop the database (caution: deletes all data)
docker exec tinyshop-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C -Q "DROP DATABASE TinyShopDB;"

# Re-run the init script
pwsh init-database.ps1
```

## Files
- `init-database.ps1` — PowerShell initialization script
- `docker-compose.yml` — Service definitions with volume mounts
- `src/Products/SQL/Setup.sql` — Main database initialization script
- `src/Products/SQL/*.sql` — Supporting scripts for tables, procedures, data

## SQL Server Tools Available in Container
- sqlcmd at `/opt/mssql-tools18/bin/sqlcmd`
- Direct queries: `docker exec tinyshop-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${MSSQL_SA_PASSWORD}' -C -Q "your query"`

## Environment Variables
The script reads credentials from `.env`:
```
PRODUCTS_DB_CONNECTION_STRING=Server=sqlserver,1433;Database=TinyShopDB;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;
```
