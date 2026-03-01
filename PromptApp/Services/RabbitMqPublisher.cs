using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PromptApp.Services;
public class RabbitMqPublisher
{
    private IConnection? _connection;
    private IChannel? _channel;

    private RabbitMqPublisher() { }

    public static async Task<RabbitMqPublisher> CreateAsync()
    {
        var publisher = new RabbitMqPublisher();
        await publisher.InitializeAsync();
        return publisher;
    }

    private async Task InitializeAsync()
    {
        var factory = new ConnectionFactory 
        { 
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
            UserName = "user",
            Password = "password"
        };

        for (int i = 0; i < 10; i++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                Console.WriteLine("Connected to RabbitMQ");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ not ready, retrying in 3s... ({i+1}/10): {ex.Message}");
                await Task.Delay(3000);
            }
        }

        if (_connection == null)
            throw new Exception("Could not connect to RabbitMQ after 10 attempts");

        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "prompt_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public async Task PublishAsync<T>(T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        if(_channel == null)
        {
            return;
        }

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "prompt_queue",
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body);
    }
}