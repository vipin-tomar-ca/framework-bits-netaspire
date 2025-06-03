using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Infrastructure.Messaging
{
    public class RabbitMQService
    {
        private readonly IBus _bus;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            var connectionString = configuration.GetConnectionString("RabbitMQ");
            _bus = RabbitHutch.CreateBus(connectionString);
            _logger = logger;
        }

        public void Publish<T>(T message, string topic)
        {
            _bus.PubSub.Publish(message, topic);
            _logger.LogInformation($"Published message to topic: {topic}");
        }

        public void Subscribe<T>(string topic, Action<T> handler)
        {
            _bus.PubSub.Subscribe<T>(topic, handler);
            _logger.LogInformation($"Subscribed to topic: {topic}");
        }

        public void Dispose()
        {
            _bus.Dispose();
        }
    }
} 