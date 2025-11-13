#!/bin/bash
set -e

echo "🚀 Starting Keycloak..."

# Use Render's PORT variable
export KC_HTTP_PORT="${PORT:-8080}"

# Use your existing keycloak_db database
export KC_DB_URL_DATABASE="${KC_TARGET_DB:-keycloak_db}"

# Build JDBC URL (Keycloak requires this format)
export KC_DB_URL="jdbc:postgresql://${KC_DB_URL_HOST}:${KC_DB_URL_PORT:-5432}/${KC_DB_URL_DATABASE}"

echo "📋 Database: ${KC_DB_URL_DATABASE}"
echo "📋 JDBC URL: ${KC_DB_URL}"
echo "📋 HTTP Port: ${KC_HTTP_PORT}"

# Start Keycloak
echo "🔐 Starting Keycloak server..."
exec /opt/keycloak/bin/kc.sh start \
  --http-enabled=true \
  --http-port=${KC_HTTP_PORT} \
  --hostname-strict=false \
  --proxy=edge