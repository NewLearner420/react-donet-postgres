FROM quay.io/keycloak/keycloak:23.0

USER root

# Install PostgreSQL client directly
RUN microdnf install -y postgresql && microdnf clean all

# Copy scripts
COPY init-db.sh /opt/keycloak/init-db.sh
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/init-db.sh /opt/keycloak/entrypoint.sh

USER 1000

# Build Keycloak with PostgreSQL support
RUN /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]