#!/bin/bash
set -e

echo "🔍 Initializing databases..."

# Create keycloak_db if it doesn't exist
echo "Checking keycloak_db..."
PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -tc "SELECT 1 FROM pg_database WHERE datname = 'keycloak_db'" | grep -q 1 && echo "✅ keycloak_db exists" || {
    echo "Creating keycloak_db..."
    PGPASSWORD="${KC_DB_PASSWORD}" psql \
      -h "${KC_DB_URL_HOST}" \
      -p "${KC_DB_URL_PORT:-5432}" \
      -U "${KC_DB_USERNAME}" \
      -d "${KC_DB_URL_DATABASE}" \
      -c "CREATE DATABASE keycloak_db;"
    echo "✅ keycloak_db created"
  }

# Create crud_users_db if it doesn't exist
echo "Checking crud_users_db..."
PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -tc "SELECT 1 FROM pg_database WHERE datname = 'crud_users_db'" | grep -q 1 && echo "✅ crud_users_db exists" || {
    echo "Creating crud_users_db..."
    PGPASSWORD="${KC_DB_PASSWORD}" psql \
      -h "${KC_DB_URL_HOST}" \
      -p "${KC_DB_URL_PORT:-5432}" \
      -U "${KC_DB_USERNAME}" \
      -d "${KC_DB_URL_DATABASE}" \
      -c "CREATE DATABASE crud_users_db;"
    echo "✅ crud_users_db created"
  }

echo "✅ Database initialization complete!"