# Stage 1: Get psql from standard Debian-based postgres image
FROM postgres:15 AS postgres-builder

# Stage 2: Build Keycloak
FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy PostgreSQL client binaries and libraries from Debian postgres
COPY --from=postgres-builder /usr/bin/psql /usr/bin/psql
COPY --from=postgres-builder /usr/lib/x86_64-linux-gnu/libpq.so* /usr/lib64/
COPY --from=postgres-builder /usr/lib/x86_64-linux-gnu/libpgcommon* /usr/lib64/
COPY --from=postgres-builder /usr/lib/x86_64-linux-gnu/libpgport* /usr/lib64/

# Copy scripts
COPY init-db.sh /opt/keycloak/init-db.sh
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/init-db.sh /opt/keycloak/entrypoint.sh

USER 1000

# Build Keycloak with PostgreSQL support
RUN /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]