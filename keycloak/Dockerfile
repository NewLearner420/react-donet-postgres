FROM quay.io/keycloak/keycloak:23.0

USER root

# Install PostgreSQL client
RUN microdnf install -y postgresql15 && microdnf clean all

COPY init-db.sh /opt/keycloak/init-db.sh
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/init-db.sh /opt/keycloak/entrypoint.sh

USER 1000

# Build Keycloak during image build
RUN /opt/keycloak/bin/kc.sh build --db=postgres

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]