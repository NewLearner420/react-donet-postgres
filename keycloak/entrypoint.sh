#!/bin/bash
set -e

echo "🚀 Starting Keycloak initialization..."

# Run database initialization script
/opt/keycloak/init-databases.sh

# Now start Keycloak with the original entrypoint
echo "🔐 Starting Keycloak server..."
exec /opt/keycloak/bin/kc.sh "$@"