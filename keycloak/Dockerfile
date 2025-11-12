FROM quay.io/keycloak/keycloak:23.0

# Install postgresql client for database initialization
USER root

# Install postgresql client
RUN microdnf install -y --nodocs postgresql && \
    microdnf clean all

# Copy initialization script
COPY init-databases.sh /opt/keycloak/init-databases.sh
RUN chmod +x /opt/keycloak/init-databases.sh

# Copy entrypoint wrapper
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]
CMD ["start"]