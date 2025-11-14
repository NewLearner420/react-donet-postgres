#!/bin/bash
set -e

echo "🚀 Starting Keycloak initialization..."

PORT="${PORT:-8080}"
DB_NAME="${KC_TARGET_DB:-keycloak_db}"
DB_URL="jdbc:postgresql://${KC_DB_URL_HOST}:${KC_DB_URL_PORT:-5432}/${DB_NAME}"

echo "📋 Database URL: ${DB_URL}"
echo "📋 Database User: ${KC_DB_USERNAME}"
echo "📋 HTTP Port: ${PORT}"

# Initialize databases first
/opt/keycloak/init-db.sh

echo "🎯 Starting Keycloak server on 0.0.0.0:${PORT}..."

# Start Keycloak
exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --db-url="${DB_URL}" \
  --db-username="${KC_DB_USERNAME}" \
  --db-password="${KC_DB_PASSWORD}" \
  --http-port="${PORT}" \
  --http-host=0.0.0.0 \
  --hostname-strict=false \
  --proxy=edge \
  --http-enabled=true