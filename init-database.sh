#!/bin/bash
# Run this script to initialize the database after docker-compose up

CONTAINER_NAME="tinyshop-sqlserver"

# Load password from .env
if [ -f .env ]; then
  SA_PASSWORD=$(grep '^MSSQL_SA_PASSWORD=' .env | cut -d'=' -f2)
else
  echo "Error: .env file not found"
  exit 1
fi

if [ -z "$SA_PASSWORD" ]; then
  echo "Error: MSSQL_SA_PASSWORD not found in .env"
  exit 1
fi

echo "Waiting for SQL Server to be ready..."
for i in {1..30}; do
  if docker exec $CONTAINER_NAME /opt/mssql/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" &>/dev/null; then
    echo "SQL Server is ready!"
    break
  fi
  echo "Attempt $i/30..."
  sleep 2
done

echo "Running Setup.sql..."
docker exec -i $CONTAINER_NAME /opt/mssql/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -i /usr/src/sql/init/Setup.sql
echo "Database initialization complete!"
