#!/bin/bash
set -e

echo "Starting Keycloak with Neon PostgreSQL..."

# Start Keycloak
exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge \
  --health-enabled=true