FROM quay.io/keycloak/keycloak:23.0

# Set working directory
WORKDIR /opt/keycloak

# Configure Keycloak for production
ENV KC_HTTP_RELATIVE_PATH=/
ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true
ENV KC_DB=postgres

# Build the optimized Keycloak image
RUN /opt/keycloak/bin/kc.sh build

# Expose port
EXPOSE 8080

# Start Keycloak in production mode
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start", "--optimized", "--proxy=edge", "--hostname-strict=false", "--http-enabled=true"]