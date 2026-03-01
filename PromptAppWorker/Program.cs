using PromptApp.Data;
using PromptApp.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<PromptAppDbContext>(options =>
            options.UseSqlite("Data Source=/app/data/prompts.db"));
    })
    .Build();

var serviceProvider = host.Services;

using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PromptAppDbContext>();
    await dbContext.Database.MigrateAsync();
}

var factory = new ConnectionFactory 
{ 
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    UserName = "user",
    Password = "password"
};

IConnection connection = null;
for (int i = 0; i < 10; i++)
{
    try
    {
        connection = await factory.CreateConnectionAsync();
        Console.WriteLine("Connected to RabbitMQ");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"RabbitMQ not ready, retrying in 3s... ({i+1}/10): {ex.Message}");
        await Task.Delay(3000);
    }
}

if (connection == null)
    throw new Exception("Could not connect to RabbitMQ after 10 attempts");

await using var _ = connection;
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(
    queue: "prompt_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

Console.WriteLine("Waiting for prompts...");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var task = JsonSerializer.Deserialize<Prompt>(message);
    if (task == null) return;

    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PromptAppDbContext>();
    var prompt = await dbContext.Prompts.FindAsync(task.Id);
    if (prompt == null) return;

    try
    {
        prompt.State = "Processing";
        Console.WriteLine($"Processing prompt {prompt.Id}");

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var apiKey = configuration["OpenAI:ApiKey"];
        var client = new OpenAIClient(apiKey);
        var chatClient = client.GetChatClient("gpt-3.5-turbo");

        var response = await chatClient.CompleteChatAsync(
            new[]
            {
                new UserChatMessage($"{prompt.Content}")
            });

        prompt.Result = response.Value.Content[0].Text;
        prompt.State = "Done";
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Task {prompt.Id} done: {prompt.Result}");
    }
    catch (Exception ex)
    {
        prompt.State = "Failed";
        prompt.Result = ex.Message;
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Task {prompt.Id} failed: {ex.Message}");
    }
    finally
    {
        await ((AsyncEventingBasicConsumer)sender)
            .Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
    }
};

await channel.BasicConsumeAsync("prompt_queue", autoAck: false, consumer);

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; tcs.SetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => tcs.SetResult();

await tcs.Task;