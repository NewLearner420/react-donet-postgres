#!/bin/bash
set -e

echo "🚀 Starting Keycloak..."

# Update the database URL to use keycloak_db
export KC_DB_URL_DATABASE="${KC_TARGET_DB:-keycloak_db}"

# Set the HTTP port for Render
export KC_HTTP_PORT="${PORT:-8080}"
export KC_HOSTNAME_PORT="${PORT:-8080}"

echo "📋 Using database: ${KC_DB_URL_DATABASE}"
echo "📋 Binding to port: ${KC_HTTP_PORT}"

# Start Keycloak with optimized flag after first run
if [ -f "/opt/keycloak/data/h2/keycloakdb.mv.db" ] || [ -f "/opt/keycloak/.build-complete" ]; then
  echo "🔐 Starting Keycloak server (optimized)..."
  exec /opt/keycloak/bin/kc.sh start --optimized --http-port=${KC_HTTP_PORT}
else
  echo "🔐 Starting Keycloak server (first run)..."
  /opt/keycloak/bin/kc.sh start --http-port=${KC_HTTP_PORT}
  # Mark build as complete
  touch /opt/keycloak/.build-complete
fi