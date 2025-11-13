FROM quay.io/keycloak/keycloak:23.0

USER root

COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

RUN rm -rf /opt/keycloak/conf/*

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]