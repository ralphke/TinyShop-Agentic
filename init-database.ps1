#!/usr/bin/env pwsh
# Run this script to initialize the database after docker-compose up

$containerName = "tinyshop-sqlserver"
$maxRetries = 30
$retryDelaySeconds = 2
$sqlcmdPath = "/opt/mssql-tools18/bin/sqlcmd"

# Load password from .env
if (Test-Path .env) {
    $envContent = Get-Content .env | Where-Object { $_ -match '^MSSQL_SA_PASSWORD=' }
    $saPassword = $envContent -split '=' | Select-Object -Last 1
} else {
    Write-Error ".env file not found"
    exit 1
}

if ([string]::IsNullOrEmpty($saPassword)) {
    Write-Error "MSSQL_SA_PASSWORD not found in .env"
    exit 1
}

Write-Host "Waiting for SQL Server to be ready..."
$connectionSuccessful = $false
for ($i = 1; $i -le $maxRetries; $i++) {
    try {
        $result = docker exec $containerName $sqlcmdPath -S localhost -U sa -P "$saPassword" -Q "SELECT 1" -C 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SQL Server is ready!"
            $connectionSuccessful = $true
            break
        }
    } catch {
        # Ignore errors and retry
    }
    Write-Host "Attempt $i/$maxRetries..."
    Start-Sleep -Seconds $retryDelaySeconds
}

if (-not $connectionSuccessful) {
    Write-Error "Failed to connect to SQL Server after $maxRetries attempts"
    exit 1
}

Write-Host "Running Setup.sql..."
$result = docker exec $containerName $sqlcmdPath -S localhost -U sa -P "$saPassword" -C -i "/usr/src/sql/init/Setup.sql" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Database initialization complete!"
    Write-Host "Successfully created TinyShopDB with schema and initial data."
} else {
    Write-Error "Database initialization failed with exit code $LASTEXITCODE"
    Write-Host $result
    exit $LASTEXITCODE
}
