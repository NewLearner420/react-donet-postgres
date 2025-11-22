FROM quay.io/keycloak/keycloak:23.0

USER root

COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

# Build phase with health endpoints enabled
RUN /opt/keycloak/bin/kc.sh build \
    --db=postgres \
    --cache=local \
    --health-enabled=true

EXPOSE 8080

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]