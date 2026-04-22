#!/bin/bash
set -u

SQLCMD="/opt/mssql-tools/bin/sqlcmd"
PASSWORD="${MSSQL_SA_PASSWORD:-}"
EMBEDDINGS_URL="${EMBEDDINGS_URL:-http://embeddings:80/embed}"
LOG_FILE="/tmp/init-db-embed-errors.log"
SERVER="sqlserver"
DATABASE="TinyShopDB"

if [ -z "$PASSWORD" ]; then
  echo "MSSQL_SA_PASSWORD is not set"
  exit 1
fi

echo "Waiting for SQL Server to be ready..."
start_time=$(date +%s)
max_wait=300
while true; do
  if $SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; then
    echo "SQL Server is ready."
    break
  fi

  now=$(date +%s)
  if [ $((now - start_time)) -ge $max_wait ]; then
    echo "Timed out waiting for SQL Server after $max_wait seconds"
    exit 1
  fi

  echo "Waiting for SQL Server..."
  sleep 2
done

echo "Waiting for embedding service to be available at $EMBEDDINGS_URL..."
start_time=$(date +%s)
max_wait=300
while true; do
  if python3 - <<'PY'
import os
import sys
import urllib.request

url = os.environ['EMBEDDINGS_URL']
try:
    req = urllib.request.Request(url, data=b'{"inputs":"ping"}', headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=10) as resp:
        if resp.status == 200:
            sys.exit(0)
except Exception:
    sys.exit(1)
PY
  then
    echo "Embedding service is available."
    break
  fi

  now=$(date +%s)
  if [ $((now - start_time)) -ge $max_wait ]; then
    echo "Timed out waiting for embedding service after $max_wait seconds"
    exit 1
  fi

  echo "Waiting for embedding service..."
  sleep 2
done

echo "Clearing previous embedding error log: $LOG_FILE"
rm -f "$LOG_FILE"
