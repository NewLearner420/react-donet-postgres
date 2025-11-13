#!/bin/bash
set -e

echo "🚀 Starting Keycloak..."

# Render provides PORT variable
PORT="${PORT:-8080}"

# Database
DB_NAME="${KC_TARGET_DB:-keycloak_db}"
DB_URL="jdbc:postgresql://${KC_DB_URL_HOST}:${KC_DB_URL_PORT:-5432}/${DB_NAME}"

echo "📋 Database URL: ${DB_URL}"
echo "📋 Database User: ${KC_DB_USERNAME}"
echo "📋 HTTP Port: ${PORT}"

# Start Keycloak with only supported runtime flags
exec /opt/keycloak/bin/kc.sh start \
  --db=postgres \
  --db-url="${DB_URL}" \
  --db-username="${KC_DB_USERNAME}" \
  --db-password="${KC_DB_PASSWORD}" \
  --http-port="${PORT}" \
  --http-host=0.0.0.0 \
  --proxy=edge

