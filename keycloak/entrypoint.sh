#!/bin/bash
set -e

echo "Starting Keycloak with Neon PostgreSQL..."
echo "PORT env variable is: ${PORT}"

exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --http-host=0.0.0.0 \
  --http-port="${PORT:-10000}" \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge