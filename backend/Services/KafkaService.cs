using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace backend.Services
{
    public class KafkaService : IKafkaService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _bootstrapServers;
        private readonly ILogger<KafkaService> _logger;

        public KafkaService(
            IProducer<string, string> producer,
            IConfiguration configuration,
            ILogger<KafkaService> logger)
        {
            _producer = producer;
            _bootstrapServers = configuration.GetConnectionString("Kafka") ?? "localhost:9092";
            _logger = logger;
        }

        public async Task<bool> ProduceMessageAsync(string topic, string key, string message)
        {
            if (_producer == null)
            {
                _logger.LogWarning("Kafka producer not initialized");
                return false;
            }

            try
            {
                var result = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = message,
                    Timestamp = Timestamp.Default
                });

                _logger.LogInformation($"✅ Delivered to {result.TopicPartitionOffset}");
                return true;
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError($"❌ Produce failed: {ex.Error.Reason}");
                return false;
            }
        }

        public async Task<List<string>> ConsumeMessagesAsync(string topic, int maxMessages = 10)
        {
            var messages = new List<string>();
            
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = $"test-consumer-{Guid.NewGuid()}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                while (messages.Count < maxMessages && !cts.Token.IsCancellationRequested)
                {
                    var result = consumer.Consume(cts.Token);
                    messages.Add(result.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout reached
            }
            finally
            {
                consumer.Close();
            }

            return messages;
        }

        public async Task<List<string>> GetTopicsAsync()
        {
            try
            {
                using var adminClient = new AdminClientBuilder(new AdminClientConfig 
                { 
                    BootstrapServers = _bootstrapServers 
                }).Build();
                
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                return metadata.Topics.Select(t => t.Topic).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get topics: {ex.Message}");
                return new List<string>();
            }
        }

        public bool IsHealthy()
        {
            return _producer != null;
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}