#!/usr/bin/env bash
# init-db startup script
set -eu

SQLCMD="sqlcmd"
PASSWORD="${MSSQL_SA_PASSWORD:-}"
LOG_FILE="/tmp/init-db-embed-errors.log"
SERVER="sqlserver"
DATABASE="TinyShopDB"
SCRIPT_DIR="/usr/src/sql/init"

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd not found in PATH. Locating sqlcmd..."
  if [ -x "/opt/mssql-tools/bin/sqlcmd" ]; then
    export PATH="/opt/mssql-tools/bin:$PATH"
    echo "Added /opt/mssql-tools/bin to PATH"
  else
    FOUND_SQLCMD=$(find / -type f -name sqlcmd 2>/dev/null | head -n 1 || true)
    if [ -n "$FOUND_SQLCMD" ] && [ -x "$FOUND_SQLCMD" ]; then
      export PATH="$(dirname "$FOUND_SQLCMD"):$PATH"
      echo "Added $(dirname "$FOUND_SQLCMD") to PATH"
    else
      echo "ERROR: sqlcmd executable not found. Install mssql-tools or verify sqlcmd is available in PATH."
      exit 1
    fi
  fi
fi

if [ -z "$PASSWORD" ]; then
  echo "MSSQL_SA_PASSWORD is not set"
  exit 1
fi

echo "Waiting for SQL Server to be ready (with extended timeout for sa user initialization)..."
start_time=$(date +%s)
max_wait=300
connection_attempts=0

while true; do
  connection_attempts=$((connection_attempts + 1))
  
  # Try to connect and run a simple query
  if $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; then
    echo "SQL Server is ready and sa user is accessible (attempt $connection_attempts)."
    break
  fi

  now=$(date +%s)
  elapsed=$((now - start_time))
  
  if [ $elapsed -ge $max_wait ]; then
    echo "ERROR: Timed out waiting for SQL Server after $max_wait seconds"
    echo "Last connection attempt: $connection_attempts"
    $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -Q "SELECT 1" 2>&1 || true
    exit 1
  fi

  if [ $((connection_attempts % 5)) -eq 0 ]; then
    echo "Waiting for SQL Server... (attempt $connection_attempts, elapsed ${elapsed}s)"
  fi
  sleep 2
done

echo "Clearing previous embedding error log: $LOG_FILE"
rm -f "$LOG_FILE"

# ── Database Initialization ──────────────────────────────────────────────────
echo "Running database setup scripts..."

# Check if database exists - use simple query without grep to avoid character issues
echo "Checking if database $DATABASE exists..."
DB_CHECK=$($SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '$DATABASE';" 2>&1 || echo "ERROR")

if [[ "$DB_CHECK" == "ERROR" ]] || [[ -z "$DB_CHECK" ]]; then
  echo "ERROR: Could not connect to SQL Server to check database existence"
  echo "Connection check output: $DB_CHECK"
  exit 1
fi

# Trim whitespace
DB_CHECK=$(echo "$DB_CHECK" | xargs)

if [ "$DB_CHECK" -ne 0 ]; then
    echo "Database $DATABASE already exists."
  exit 0
fi

# Run setup script
if [ -f "$SCRIPT_DIR/Setup.sql" ]; then
  echo "Running Setup.sql..."
  if ! $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -i "$SCRIPT_DIR/Setup.sql" 2>&1 | tee -a "$LOG_FILE"; then
    echo "Warning: Setup.sql had errors (may be non-critical)"
  fi
else
  echo "Warning: Setup.sql not found at $SCRIPT_DIR/Setup.sql"
fi

echo ""
echo "Database initialization complete!"
echo "Log file: $LOG_FILE"
if [ -f "$LOG_FILE" ] && [ -s "$LOG_FILE" ]; then
  echo "Errors/warnings logged to: $LOG_FILE"
  echo "---"
  cat "$LOG_FILE" || echo "Could not read log file"
fi
