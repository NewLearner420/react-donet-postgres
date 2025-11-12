#!/bin/bash
set -e

echo "🚀 Starting Keycloak initialization..."

# Run database initialization script
/opt/keycloak/init-databases.sh

# Update the database URL to use keycloak_db instead of main_db
export KC_DB_URL_DATABASE="${KC_TARGET_DB:-keycloak_db}"

# Now start Keycloak with the original entrypoint
echo "🔐 Starting Keycloak server..."
exec /opt/keycloak/bin/kc.sh "$@"