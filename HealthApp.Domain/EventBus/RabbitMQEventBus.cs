using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace HealthApp.Domain.EventBus;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = "health_app_exchange";
    private readonly ILogger<RabbitMQEventBus> _logger;

    public RabbitMQEventBus(string hostname, ILogger<RabbitMQEventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostname,
                DispatchConsumersAsync = true // Enable async consumer support
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public void Publish(string queueName, string message)
    {
        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));

        try
        {
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: queueName,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message published to queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to queue {QueueName}", queueName);
            throw;
        }
    }

    public void Subscribe(string queueName, Action<string> handler)
    {
        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        try
        {
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: queueName,
                arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    handler(message);
                    _logger.LogDebug("Message processed from queue {QueueName}", queueName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    // Consider implementing dead-letter queue for failed messages
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer);

            _logger.LogInformation("Subscribed to queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to queue {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}