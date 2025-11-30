#!/bin/bash
set -e

echo "🚀 Starting Kafka in KRaft mode..."

# Environment variables with defaults
KAFKA_NODE_ID=${KAFKA_NODE_ID:-1}
KAFKA_PROCESS_ROLES=${KAFKA_PROCESS_ROLES:-"broker,controller"}
KAFKA_LISTENERS=${KAFKA_LISTENERS:-"PLAINTEXT://:9092,CONTROLLER://:9093,EXTERNAL://:9094"}
KAFKA_ADVERTISED_LISTENERS=${KAFKA_ADVERTISED_LISTENERS:-"PLAINTEXT://localhost:9092,EXTERNAL://localhost:9094"}
KAFKA_CONTROLLER_QUORUM_VOTERS=${KAFKA_CONTROLLER_QUORUM_VOTERS:-"1@localhost:9093"}
KAFKA_LOG_DIRS=${KAFKA_LOG_DIRS:-"/tmp/kraft-combined-logs"}
CLUSTER_ID=${CLUSTER_ID:-$(${KAFKA_HOME}/bin/kafka-storage.sh random-uuid)}

echo "📝 Kafka Configuration:"
echo "   Node ID: ${KAFKA_NODE_ID}"
echo "   Cluster ID: ${CLUSTER_ID}"

# Create server.properties
cat > ${KAFKA_HOME}/config/kraft/server.properties << EOF
node.id=${KAFKA_NODE_ID}
process.roles=${KAFKA_PROCESS_ROLES}
listeners=${KAFKA_LISTENERS}
advertised.listeners=${KAFKA_ADVERTISED_LISTENERS}
listener.security.protocol.map=${KAFKA_LISTENER_SECURITY_PROTOCOL_MAP}
controller.quorum.voters=${KAFKA_CONTROLLER_QUORUM_VOTERS}
controller.listener.names=${KAFKA_CONTROLLER_LISTENER_NAMES}
inter.broker.listener.name=${KAFKA_INTER_BROKER_LISTENER_NAME}
log.dirs=${KAFKA_LOG_DIRS}
num.network.threads=3
num.io.threads=8
socket.send.buffer.bytes=102400
socket.receive.buffer.bytes=102400
socket.request.max.bytes=104857600
num.partitions=${KAFKA_NUM_PARTITIONS:-1}
num.recovery.threads.per.data.dir=1
offsets.topic.replication.factor=1
transaction.state.log.replication.factor=1
transaction.state.log.min.isr=1
log.retention.hours=${KAFKA_LOG_RETENTION_HOURS:-168}
log.segment.bytes=${KAFKA_LOG_SEGMENT_BYTES:-1073741824}
log.retention.check.interval.ms=300000
metadata.log.dir=${KAFKA_LOG_DIRS}
auto.create.topics.enable=${KAFKA_AUTO_CREATE_TOPICS_ENABLE:-true}
EOF

# Format storage if needed
if [ ! -f "${KAFKA_LOG_DIRS}/meta.properties" ]; then
    echo "📦 Formatting storage..."
    ${KAFKA_HOME}/bin/kafka-storage.sh format \
        -t ${CLUSTER_ID} \
        -c ${KAFKA_HOME}/config/kraft/server.properties
    echo "✅ Storage formatted"
else
    echo "✅ Storage already formatted"
fi

# Start Kafka
echo "🚀 Starting Kafka server..."
exec ${KAFKA_HOME}/bin/kafka-server-start.sh ${KAFKA_HOME}/config/kraft/server.properties