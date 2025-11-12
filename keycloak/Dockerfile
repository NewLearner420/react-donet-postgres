FROM quay.io/keycloak/keycloak:23.0

USER root

# Copy entrypoint wrapper (no more init script needed)
COPY entrypoint.sh /opt/keycloak/entrypoint.sh
RUN chmod +x /opt/keycloak/entrypoint.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/entrypoint.sh"]
CMD ["start", "--http-port=8080"]