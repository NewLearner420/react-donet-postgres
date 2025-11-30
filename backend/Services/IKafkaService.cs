namespace backend.Services
{
    public interface IKafkaService
    {
        Task<bool> ProduceMessageAsync(string topic, string key, string message);
        Task<List<string>> ConsumeMessagesAsync(string topic, int maxMessages = 10);
        bool IsHealthy();
        Task<List<string>> GetTopicsAsync();
    }
}