#!/bin/sh

# Generate runtime config file
cat > /usr/share/nginx/html/config.js <<EOF
window.APP_CONFIG = {
  GRAPHQL_HTTP_URL: "${VITE_GRAPHQL_HTTP_URL:-http://localhost:8080/graphql}",
  GRAPHQL_WS_URL: "${VITE_GRAPHQL_WS_URL:-ws://localhost:8080/graphql}",
  KEYCLOAK_URL: "${VITE_KEYCLOAK_URL:-http://localhost:8090}",
  KEYCLOAK_REALM: "${VITE_KEYCLOAK_REALM:-myrealm}",
  KEYCLOAK_CLIENT_ID: "${VITE_KEYCLOAK_CLIENT_ID:-react-app}"
};
EOF

echo "✅ Runtime configuration generated:"
cat /usr/share/nginx/html/config.js