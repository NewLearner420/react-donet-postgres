FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy entrypoint script
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

# Clean stale config and rebuild Keycloak
RUN rm -rf /opt/keycloak/conf/* && \
    /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]
