#!/bin/sh

# Build connection string from individual components
export ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

# Set Keycloak configuration from service URLs
if [ -n "$KEYCLOAK_SERVICE_URL" ]; then
  export Keycloak__Authority="${KEYCLOAK_SERVICE_URL}/realms/myrealm"
  export Keycloak__MetadataAddress="${KEYCLOAK_SERVICE_URL}/realms/myrealm/.well-known/openid-configuration"
  echo "✅ Keycloak Authority: ${Keycloak__Authority}"
fi

# Set CORS origins
if [ -n "$FRONTEND_SERVICE_URL" ]; then
  export Cors__AllowedOrigins__0="${FRONTEND_SERVICE_URL}"
  echo "✅ CORS Origin: ${Cors__AllowedOrigins__0}"
fi

# Add localhost for development
export Cors__AllowedOrigins__1="http://localhost:3000"

echo "🚀 Starting GraphQL API..."
echo "📊 Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
echo "🔐 Keycloak: ${Keycloak__Authority}"
echo "🌐 CORS Origins: ${Cors__AllowedOrigins__0}, ${Cors__AllowedOrigins__1}"

# Start the application
exec dotnet backend.dll