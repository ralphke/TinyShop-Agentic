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

echo "Running Setup.sql..."
cd /usr/src/sql/init
$SQLCMD -S "$SERVER" -U sa -P "$PASSWORD" -C -i Setup.sql || echo "Warning: Setup.sql completed with warnings"

echo "Clearing previous embedding error log: $LOG_FILE"
rm -f "$LOG_FILE"

echo "Generating product embeddings..."
set +e
EMBEDDING_LOG_FILE="$LOG_FILE" python3 - <<'PY'
import json
import os
import subprocess
import sys
import time
import urllib.request

SQLCMD = "/opt/mssql-tools/bin/sqlcmd"
SERVER = os.environ.get('SERVER', 'sqlserver')
USER = 'sa'
PASSWORD = os.environ['MSSQL_SA_PASSWORD']
DATABASE = 'TinyShopDB'
EMBEDDINGS_URL = os.environ.get('EMBEDDINGS_URL', 'http://embeddings:80/embed')
LOG_FILE = os.environ.get('EMBEDDING_LOG_FILE', '/tmp/init-db-embed-errors.log')
MAX_RETRIES = 3
RETRY_DELAY = 5


def log_error(product_id, kind, message):
    timestamp = time.strftime('%Y-%m-%d %H:%M:%S')
    line = f"{timestamp} | ProductId={product_id} | {kind} | {message}\n"
    with open(LOG_FILE, 'a', encoding='utf-8') as log:
        log.write(line)


def log_failure(product_id, kind, error):
    message = str(error)
    print(message)
    log_error(product_id, kind, message)

query = (
    "SET NOCOUNT ON; "
    "SELECT Id, Name, Description, Details FROM dbo.Products FOR JSON PATH;"
)
try:
    proc = subprocess.run(
        [SQLCMD, '-S', SERVER, '-U', USER, '-P', PASSWORD, '-d', DATABASE, '-h', '-1', '-W', '-w', '65535', '-C', '-Q', query],
        capture_output=True,
        text=True,
        timeout=30,
    )
    if proc.returncode != 0:
        print(f"SQL query failed with code {proc.returncode}")
        print(f"stderr: {proc.stderr}")
        sys.exit(0)  # Continue anyway
    
    text = proc.stdout.strip()
    if not text:
        print('No products found to embed.')
        sys.exit(0)

    try:
        products = json.loads(text)
    except json.JSONDecodeError as ex:
        print('Failed to parse product JSON from SQL output:', ex)
        print('Skipping embedding generation')
        sys.exit(0)

    if not products:
        print('No products found to embed.')
        sys.exit(0)


    def parse_embedding_response(data):
        if isinstance(data, list) and data and isinstance(data[0], list):
            return data[0]
        if isinstance(data, dict):
            if 'embedding' in data and isinstance(data['embedding'], list):
                return data['embedding']
            if 'vector' in data and isinstance(data['vector'], list):
                return data['vector']
            if 'data' in data and isinstance(data['data'], list) and data['data']:
                first = data['data'][0]
                if isinstance(first, dict) and 'embedding' in first and isinstance(first['embedding'], list):
                    return first['embedding']
                if isinstance(first, list):
                    return first
            if 'embeddings' in data and isinstance(data['embeddings'], list) and data['embeddings']:
                first = data['embeddings'][0]
                if isinstance(first, list):
                    return first
                if isinstance(first, dict) and 'embedding' in first and isinstance(first['embedding'], list):
                    return first['embedding']
        raise ValueError(f'Unable to parse embedding response: {data}')


    def get_embedding(text, kind):
        if not text:
            return []

        payload = json.dumps({'inputs': text}).encode('utf-8')
        for attempt in range(1, MAX_RETRIES + 1):
            try:
                req = urllib.request.Request(EMBEDDINGS_URL, data=payload, headers={'Content-Type': 'application/json'})
                with urllib.request.urlopen(req, timeout=30) as resp:
                    if resp.status != 200:
                        raise ValueError(f'Unexpected status {resp.status}')
                    body = resp.read()
                    data = json.loads(body)
                return parse_embedding_response(data)
            except Exception as error:
                message = f'Embedding request failed for {kind} (attempt {attempt}/{MAX_RETRIES}): {error}'
                print(message)
                if attempt == MAX_RETRIES:
                    raise RuntimeError(message)
                time.sleep(RETRY_DELAY)

    updates = []
    failed_products = set()
    for product in products:
        product_id = product.get('Id')
        if product_id is None:
            continue

        name = product.get('Name') or ''
        description = product.get('Description') or ''
        details = product.get('Details') or ''
        description_text = ' '.join(part for part in [name, description, details] if part.strip())
        name_text = name.strip()

        try:
            desc_embedding = get_embedding(description_text, 'description')
        except Exception as error:
            log_failure(product_id, 'description', error)
            failed_products.add(product_id)
            desc_embedding = []

        try:
            name_embedding = get_embedding(name_text, 'name')
        except Exception as error:
            log_failure(product_id, 'name', error)
            failed_products.add(product_id)
            name_embedding = []

        desc_json = json.dumps(desc_embedding, separators=(',', ':')).replace("'", "''")
        name_json = json.dumps(name_embedding, separators=(',', ':')).replace("'", "''")
        updates.append(
            f"UPDATE dbo.Products SET DescriptionEmbedding = N'{desc_json}', NameEmbedding = N'{name_json}' WHERE Id = {product_id};"
        )

    if updates:
        sql_file = '/tmp/embed_updates.sql'
        with open(sql_file, 'w', encoding='utf-8') as f:
            f.write('SET NOCOUNT ON;\n')
            f.write('\n'.join(updates))

        subprocess.run([SQLCMD, '-S', SERVER, '-U', USER, '-P', PASSWORD, '-d', DATABASE, '-C', '-i', sql_file], check=True)
        print('Product embeddings generated successfully.')
    
    if failed_products:
        print('Embedding generation completed with some failures for products:', sorted(failed_products))
        print(f'Details saved to {LOG_FILE}')

except Exception as e:
    print(f'Error during embedding generation: {e}')
    sys.exit(0)
PY
EMBED_EXIT=$?
set -e

if [ $EMBED_EXIT -ne 0 ]; then
    echo "Warning: Embedding generation failed, but continuing"
fi

echo "Database initialization script completed successfully."
