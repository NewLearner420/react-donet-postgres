#!/bin/bash
set -e

echo "🚀 Starting Keycloak..."

# Render provides PORT variable
PORT="${PORT:-8080}"

# Build the database name (use keycloak_db)
DB_NAME="${KC_TARGET_DB:-keycloak_db}"

# Build JDBC URL with proper format
DB_URL="jdbc:postgresql://${KC_DB_URL_HOST}:${KC_DB_URL_PORT:-5432}/${DB_NAME}"

echo "📋 Database URL: ${DB_URL}"
echo "📋 Database User: ${KC_DB_USERNAME}"
echo "📋 HTTP Port: ${PORT}"

echo "🔍 Checking port binding inside container..."
netstat -tuln | grep $PORT || echo "⚠️ Nothing listening on $PORT yet"

# Start Keycloak with all settings as command args
exec /opt/keycloak/bin/kc.sh start \
  --db=postgres \
  --db-url="${DB_URL}" \
  --db-username="${KC_DB_USERNAME}" \
  --db-password="${KC_DB_PASSWORD}" \
  --http-enabled=true \
  --http-host=0.0.0.0 \
  --http-port="${PORT}" \
  --hostname-strict=false \
  --hostname-strict-https=false \
  --proxy=edge \
  --health-enabled=true

