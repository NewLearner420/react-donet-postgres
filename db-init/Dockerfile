FROM postgres:16-alpine

# Install psql client
RUN apk add --no-cache postgresql-client

# Copy initialization script
COPY init-databases.sh /init-databases.sh
RUN chmod +x /init-databases.sh

# Run the initialization script
CMD ["/init-databases.sh"]