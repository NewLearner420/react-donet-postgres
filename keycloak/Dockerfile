# Stage 1: Get postgresql client from official postgres image
FROM postgres:15-alpine AS postgres-client

# Stage 2: Build Keycloak with psql
FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy psql binary and required libraries from postgres image
COPY --from=postgres-client /usr/local/bin/psql /usr/local/bin/psql
COPY --from=postgres-client /usr/local/lib/libpq.so.5 /usr/local/lib/libpq.so.5

# Set library path
ENV LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH

# Copy initialization script
COPY init-databases.sh /opt/keycloak/init-databases.sh
RUN chmod +x /opt/keycloak/init-databases.sh

# Copy entrypoint wrapper
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]
CMD ["start"]