#!/bin/bash
set -e

echo "Starting Keycloak with Neon PostgreSQL..."
echo "PORT env variable is: ${PORT}"
echo "KC_DB_URL is: ${KC_DB_URL}"
echo "KC_DB_USERNAME is: ${KC_DB_USERNAME}"

exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --http-host=0.0.0.0 \
  --http-port="${PORT:-10000}" \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge