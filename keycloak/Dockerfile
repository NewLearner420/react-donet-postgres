FROM postgres:15-alpine AS postgres-client

FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy psql binary and libraries (use correct Alpine Linux paths)
COPY --from=postgres-client /usr/local/bin/psql /usr/local/bin/psql
COPY --from=postgres-client /usr/local/lib/libpq.so* /usr/local/lib/

# Copy scripts
COPY init-db.sh /opt/keycloak/init-db.sh
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/init-db.sh /opt/keycloak/entrypoint.sh

USER 1000

# Build Keycloak with PostgreSQL support
RUN /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]