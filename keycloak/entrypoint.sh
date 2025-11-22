#!/bin/bash
set -e

echo "Starting Keycloak..."
echo "PORT: ${PORT}"
echo "DB URL: ${KC_DB_URL}"
echo "DB User: ${KC_DB_USERNAME}"
echo "DB Password length: ${#KC_DB_PASSWORD}"

# Test network connectivity to Neon
echo "Testing network connectivity to Neon..."
if command -v nc &> /dev/null; then
  nc -zv ep-fragrant-bar-adb2cjoa.c-2.us-east-1.aws.neon.tech 5432 -w 10 && echo "Network OK" || echo "Network FAILED"
elif command -v timeout &> /dev/null; then
  timeout 10 bash -c 'cat < /dev/null > /dev/tcp/ep-fragrant-bar-adb2cjoa.c-2.us-east-1.aws.neon.tech/5432' && echo "Network OK" || echo "Network FAILED"
else
  echo "No network test tools available"
fi

sleep 5

exec /opt/keycloak/bin/kc.sh start \
  --optimized \
  --http-host=0.0.0.0 \
  --http-port="${PORT:-10000}" \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge