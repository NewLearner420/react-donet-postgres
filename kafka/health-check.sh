#!/bin/bash

${KAFKA_HOME}/bin/kafka-broker-api-versions.sh --bootstrap-server localhost:9092 > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo '{"status":"healthy"}'
    exit 0
else
    echo '{"status":"unhealthy"}'
    exit 1
fi