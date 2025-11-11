#!/bin/sh

echo "Waiting for PostgreSQL to be ready..."
until PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -c '\q'; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "PostgreSQL is up - creating databases..."

# Create keycloak_db database
PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" <<-EOSQL
    SELECT 'CREATE DATABASE keycloak_db'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak_db')\gexec
EOSQL

# Create crud_users_db database
PGPASSWORD=$PGPASSWORD psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" <<-EOSQL
    SELECT 'CREATE DATABASE crud_users_db'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'crud_users_db')\gexec
EOSQL

echo "Databases created successfully!"

# Keep the container running (as a worker)
tail -f /dev/null