FROM eclipse-temurin:17-jre-alpine

# Install bash and curl
RUN apk add --no-cache bash curl

# Download and install Kafka
ENV KAFKA_VERSION=3.6.1
ENV SCALA_VERSION=2.13
ENV KAFKA_HOME=/opt/kafka

RUN wget -q "https://archive.apache.org/dist/kafka/${KAFKA_VERSION}/kafka_${SCALA_VERSION}-${KAFKA_VERSION}.tgz" && \
    tar -xzf "kafka_${SCALA_VERSION}-${KAFKA_VERSION}.tgz" && \
    mv "kafka_${SCALA_VERSION}-${KAFKA_VERSION}" ${KAFKA_HOME} && \
    rm "kafka_${SCALA_VERSION}-${KAFKA_VERSION}.tgz"

WORKDIR ${KAFKA_HOME}

# Create directories
RUN mkdir -p /tmp/kraft-combined-logs

# Copy scripts
COPY start-kafka.sh /usr/local/bin/
COPY health-check.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/start-kafka.sh /usr/local/bin/health-check.sh

EXPOSE 9092 9093 9094

CMD ["/usr/local/bin/start-kafka.sh"]