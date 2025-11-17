#!/bin/bash
set -e

echo "🚀 Starting Keycloak initialization..."

PORT="${PORT:-8080}"
TARGET_DB="${KC_TARGET_DB:-keycloak_db}"

echo "📋 Port: ${PORT}"
echo "📋 Target DB: ${TARGET_DB}"

# Initialize databases first (creates keycloak_db and crud_users_db if needed)
/opt/keycloak/init-db.sh

# Construct the JDBC URL for Keycloak to use keycloak_db
DB_URL="jdbc:postgresql://${KC_DB_URL_HOST}:${KC_DB_URL_PORT:-5432}/${TARGET_DB}"

echo "📋 Keycloak DB URL: ${DB_URL}"
echo "🎯 Starting Keycloak server on 0.0.0.0:${PORT}..."

# Start Keycloak with the keycloak_db database
exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --db-url="${DB_URL}" \
  --db-username="${KC_DB_USERNAME}" \
  --db-password="${KC_DB_PASSWORD}" \
  --http-port="${PORT}" \
  --http-host=0.0.0.0 \
  --hostname-strict=false \
  --proxy-headers=xforwarded \
  --http-enabled=true