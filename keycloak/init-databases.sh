#!/bin/bash
set -e

echo "🔍 Checking if databases need to be created..."

# Extract connection details
PGHOST="${KC_DB_URL_HOST}"
PGPORT="${KC_DB_URL_PORT:-5432}"
PGUSER="${KC_DB_USERNAME}"
PGPASSWORD="${KC_DB_PASSWORD}"
MAIN_DB="${KC_DB_URL_DATABASE}"  # This will be main_db_i2jy from Render
TARGET_DB="${KC_TARGET_DB:-keycloak_db}"  # The database Keycloak will use

# Wait for PostgreSQL to be ready with timeout
echo "⏳ Waiting for PostgreSQL at ${PGHOST}:${PGPORT}..."
MAX_RETRIES=60
RETRY_COUNT=0

until PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "${MAIN_DB}" -c '\q' 2>/dev/null; do
  RETRY_COUNT=$((RETRY_COUNT + 1))
  if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
    echo "❌ ERROR: PostgreSQL did not become ready in time"
    exit 1
  fi
  echo "PostgreSQL is unavailable - sleeping (attempt $RETRY_COUNT/$MAX_RETRIES)"
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
create_database_if_not_exists "${TARGET_DB}"

# Create crud_users_db (API will use this)
create_database_if_not_exists "crud_users_db"

echo "🎉 Database initialization complete!"

# Now update KC_DB_URL_DATABASE to point to keycloak_db for Keycloak to use
export KC_DB_URL_DATABASE="${TARGET_DB}"