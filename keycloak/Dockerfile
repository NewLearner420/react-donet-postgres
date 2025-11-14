FROM postgres:15-alpine AS postgres-client

FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy psql binary from postgres image
COPY --from=postgres-client /usr/local/bin/psql /usr/local/bin/psql
COPY --from=postgres-client /usr/lib/libpq.so* /usr/lib/
COPY --from=postgres-client /usr/lib/postgresql/ /usr/lib/postgresql/

COPY init-db.sh /opt/keycloak/init-db.sh
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/init-db.sh /opt/keycloak/entrypoint.sh

USER 1000

# Build Keycloak during image build
RUN /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]