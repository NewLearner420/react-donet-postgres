FROM quay.io/keycloak/keycloak:23.0

USER root

COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

# Build phase with local cache
RUN /opt/keycloak/bin/kc.sh build --cache=local

EXPOSE 8080

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]