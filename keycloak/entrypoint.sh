#!/bin/bash
set -e

echo "🚀 Starting Keycloak..."

# Update the database URL to use keycloak_db
export KC_DB_URL_DATABASE="${KC_TARGET_DB:-keycloak_db}"

echo "📋 Using database: ${KC_DB_URL_DATABASE}"

# Start Keycloak
echo "🔐 Starting Keycloak server..."
exec /opt/keycloak/bin/kc.sh "$@"