# SQL Server Container with sqlpackage and .NET SDK

## Overview
The custom SQL Server 2025 container includes:
- **sqlcmd**: CLI tool for running SQL queries (v18)
- **sqlpackage**: Database deployment tool (v170.3.93)
- **.NET 8.0 SDK**: For running .NET applications in the container
- **Full .NET toolchain**: All dependencies pre-installed

## What's Installed

### sqlcmd
SQL Server command-line tool for executing T-SQL scripts and queries.

```bash
# Inside container or via docker exec
sqlcmd -S localhost -U sa -P 'password' -C -Q "SELECT @@VERSION"
```

### sqlpackage
Microsoft SQL Server Data-Tier Application deployment tool. Supports:
- **Publish**: Deploy .dacpac files to databases
- **Extract**: Extract database schema to .dacpac
- **Export**: Export database to .bacpac
- **Import**: Import .bacpac to database
- **DeployReport/DriftReport**: Generate deployment reports
- **Script**: Generate T-SQL scripts

```bash
# Inside container or via docker exec
sqlpackage /Action:Publish /SourceFile:Database.dacpac /TargetServerName:localhost /TargetDatabaseName:MyDB /TargetUser:sa /TargetPassword:password

sqlpackage /Action:Extract /TargetFile:Database.dacpac /SourceServerName:localhost /SourceDatabaseName:MyDB /SourceUser:sa /SourcePassword:password

sqlpackage /Help
```

### .NET 8.0 SDK
Full .NET development toolkit including runtime, libraries, and tooling.

```bash
dotnet --version      # 8.0.420
dotnet --list-sdks
dotnet --list-runtimes
```

## Using the Tools

### From Host Machine via docker exec

```bash
# Run sqlcmd query
docker exec tinyshop-sqlserver sqlcmd -S localhost -U sa -P 'P@ssw0rd123!' -C -Q "SELECT name FROM sys.databases"

# Publish a .dacpac
docker exec tinyshop-sqlserver sqlpackage /Action:Publish \
  /SourceFile:/path/in/container/database.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:TinyShopDB \
  /TargetUser:sa \
  /TargetPassword:'P@ssw0rd123!'

# Extract database to .dacpac
docker exec tinyshop-sqlserver sqlpackage /Action:Extract \
  /TargetFile:/tmp/TinyShopDB.dacpac \
  /SourceServerName:localhost \
  /SourceDatabaseName:TinyShopDB \
  /SourceUser:sa \
  /SourcePassword:'P@ssw0rd123!'
```

### Inside Container via docker exec -it

```bash
# Get interactive shell
docker exec -it tinyshop-sqlserver bash

# Then use tools directly
sqlcmd -S localhost -U sa -P 'P@ssw0rd123!' -C
sqlpackage /Help
dotnet --version
```

### Copy Files In/Out

```bash
# Copy .dacpac into container
docker cp Database.dacpac tinyshop-sqlserver:/tmp/

# Copy extracted .dacpac from container
docker cp tinyshop-sqlserver:/tmp/Database.dacpac ./

# Copy generated SQL script
docker cp tinyshop-sqlserver:/tmp/deploy.sql ./
```

## Database Initialization

Initialize the database after container startup:

```bash
pwsh init-database.ps1
```

This script:
1. Waits for SQL Server to be ready
2. Runs `Setup.sql` to create TinyShopDB
3. Creates all schema, tables, indexes
4. Populates initial data

## Dockerfile Configuration

The custom Dockerfile includes:

```dockerfile
FROM mcr.microsoft.com/mssql/server:2025-latest

# Install dependencies + unzip
RUN apt-get install -y curl wget unzip ...

# Install .NET SDK 8.0
RUN ./dotnet-install.sh --channel 8.0 --install-dir /usr/local/dotnet

# Install sqlpackage standalone
RUN curl -L https://aka.ms/sqlpackage-linux -o /tmp/sqlpackage.zip
RUN unzip /tmp/sqlpackage.zip -d /opt/sqlpackage

# Add to PATH
RUN ln -s /usr/local/dotnet/dotnet /usr/local/bin/dotnet
RUN ln -s /opt/sqlpackage/sqlpackage /usr/local/bin/sqlpackage
```

## Container Size Impact

The additions add approximately **300 MB** to the base SQL Server image:
- .NET 8.0 SDK: ~230 MB
- sqlpackage: ~50 MB
- Supporting libraries: ~20 MB

## Troubleshooting

### Tool Not Found
If sqlpackage or dotnet aren't accessible:
```bash
docker exec tinyshop-sqlserver which sqlpackage
docker exec tinyshop-sqlserver which dotnet
```

### SSL Certificate Errors with sqlcmd
Use the `-C` flag to skip certificate verification for local development:
```bash
sqlcmd -S localhost -U sa -P 'password' -C -Q "SELECT 1"
```

### sqlpackage Connection Issues
Ensure SQL Server is ready before running sqlpackage. Use the health check status:
```bash
docker inspect tinyshop-sqlserver | grep -A 5 '"Health"'
```

## Common sqlpackage Workflows

### Create a Backup (.bacpac)
```bash
docker exec tinyshop-sqlserver sqlpackage /Action:Export \
  /SourceServerName:localhost \
  /SourceDatabaseName:TinyShopDB \
  /SourceUser:sa \
  /SourcePassword:'P@ssw0rd123!' \
  /TargetFile:/tmp/TinyShopDB_backup.bacpac
```

### Deploy Schema from Existing Database
```bash
# Extract production database to .dacpac
sqlpackage /Action:Extract /SourceServerName:prodserver /SourceDatabaseName:ProdDB \
  /SourceUser:sa /SourcePassword:pwd /TargetFile:ProdDB.dacpac

# Publish to container
docker cp ProdDB.dacpac tinyshop-sqlserver:/tmp/

docker exec tinyshop-sqlserver sqlpackage /Action:Publish \
  /SourceFile:/tmp/ProdDB.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:DeployedDB \
  /TargetUser:sa \
  /TargetPassword:'P@ssw0rd123!'
```

### Generate Upgrade Script
```bash
docker exec tinyshop-sqlserver sqlpackage /Action:Script \
  /SourceFile:UpdatedDatabase.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:TinyShopDB \
  /TargetUser:sa \
  /TargetPassword:'P@ssw0rd123!' \
  /OutputPath:/tmp/upgrade.sql
```

## Files
- `Dockerfile.sqlserver` — Custom image definition
- `docker-compose.yml` — Service configuration
- `init-database.ps1` — Database initialization script
- `DATABASE_INIT.md` — Database setup documentation
