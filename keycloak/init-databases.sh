#!/bin/bash
set -e

echo "🔍 Checking if databases need to be created..."

# Extract connection details from KC_DB_URL_HOST, etc.
PGHOST="${KC_DB_URL_HOST}"
PGPORT="${KC_DB_URL_PORT:-5432}"
PGUSER="${KC_DB_USERNAME}"
PGPASSWORD="${KC_DB_PASSWORD}"
MAIN_DB="${KC_DB_URL_DATABASE}"

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL at ${PGHOST}:${PGPORT}..."
until PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "${MAIN_DB}" -c '\q' 2>/dev/null; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "✅ PostgreSQL is ready!"

# Function to create database if it doesn't exist
create_database_if_not_exists() {
  local db_name=$1
  echo "📊 Checking database: ${db_name}"
  
  PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "${MAIN_DB}" <<-EOSQL
    SELECT 'CREATE DATABASE ${db_name}'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${db_name}')\gexec
EOSQL
  
  if [ $? -eq 0 ]; then
    echo "✅ Database ${db_name} is ready"
  else
    echo "⚠️  Warning: Could not verify ${db_name}"
  fi
}

# Create keycloak_db (Keycloak will use this)
create_database_if_not_exists "keycloak_db"

# Create crud_users_db (API will use this)
create_database_if_not_exists "crud_users_db"

echo "🎉 Database initialization complete!"