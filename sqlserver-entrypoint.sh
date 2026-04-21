#!/bin/bash
set -e

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &
SERVER_PID=$!

# Wait for SQL Server to be ready with TCP check
echo "Waiting for SQL Server to start..."
for i in {1..60}; do
  if exec 3<>/dev/tcp/127.0.0.1/1433 2>/dev/null; then
    exec 3>&-
    echo "SQL Server is ready!"
    break
  fi
  echo "Attempt $i/60: Waiting for SQL Server..."
  sleep 2
done

# Keep SQL Server running in foreground
wait $SERVER_PID
