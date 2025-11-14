#!/bin/bash
set -e

echo "🔍 Checking if keycloak_db exists..."

# Connect to the default database and create keycloak_db if it doesn't exist
PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -tc "SELECT 1 FROM pg_database WHERE datname = 'keycloak_db'" | grep -q 1 || \
PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -c "CREATE DATABASE keycloak_db;"

echo "✅ keycloak_db is ready!"

# Also create crud_users_db for your application if it doesn't exist
echo "🔍 Checking if crud_users_db exists..."

PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -tc "SELECT 1 FROM pg_database WHERE datname = 'crud_users_db'" | grep -q 1 || \
PGPASSWORD="${KC_DB_PASSWORD}" psql \
  -h "${KC_DB_URL_HOST}" \
  -p "${KC_DB_URL_PORT:-5432}" \
  -U "${KC_DB_USERNAME}" \
  -d "${KC_DB_URL_DATABASE}" \
  -c "CREATE DATABASE crud_users_db;"

echo "✅ crud_users_db is ready!"