#!/bin/bash
set -e

# TEMPORARY - Remove after testing!
export KC_DB_URL="jdbc:postgresql://ep-fragrant-bar-adb2cjoa.c-2.us-east-1.aws.neon.tech/neondb?sslmode=require"
export KC_DB_USERNAME="neondb_owner"
export KC_DB_PASSWORD="your-new-rotated-password"

echo "Starting Keycloak..."
echo "PORT: ${PORT}"
echo "DB URL: ${KC_DB_URL}"
echo "DB User: ${KC_DB_USERNAME}"

sleep 15

exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --http-host=0.0.0.0 \
  --http-port="${PORT:-10000}" \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge