#!/bin/bash
set -e

echo "Starting Keycloak with Neon PostgreSQL..."

exec /opt/keycloak/bin/kc.sh start \
  --http-port=8080 \
  --hostname-strict=false \
  --http-enabled=true \
  --proxy=edge \
  --cache=local