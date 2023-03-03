using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQSamples.Application.Infrastructure.AmqpAdapters;

namespace RabbitMQSamples.Application.Common.Extensions;

public static class WorkerExtensions
{
    private const string EXCHANGE_NAME_SUFFIX = "_service";
    private const string QUEUE_NAME_SUFFIX = "_queue";

    /// <summary>
    /// Create a new QueueServiceWorker to bind a queue with an function
    /// </summary>
    /// <typeparam name="TService">Service Type will be used to determine which service will be used to connect on queue</typeparam>
    /// <typeparam name="TRequest">Type of message sent by publisher to consumer. Must be exactly same Type that functionToExecute parameter requests.</typeparam>
    /// <typeparam name="TResponse">Type of returned message sent by consumer to publisher. Must be exactly same Type that functionToExecute returns.</typeparam>
    /// <param name="services">Dependency Injection Service Collection</param>
    /// <param name="queueName">Name of queue</param>
    /// <param name="functionToExecute">Function to execute when any message are consumed from queue</param>
    public static void MapQueue<TService, TRequest>(
        this IServiceCollection services,
        string queueName,
        ushort prefetchCount,
        Func<TService, TRequest, Task> functionToExecute)
        where TRequest : struct
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(queueName)) throw new ArgumentException($"'{nameof(queueName)}' cannot be null or empty.", nameof(queueName));
        if (prefetchCount < 1) throw new ArgumentOutOfRangeException(nameof(prefetchCount));
        if (functionToExecute is null) throw new ArgumentNullException(nameof(functionToExecute));

        services.AddSingleton<IHostedService>(sp =>
                new AsyncQueueServiceWorker<TRequest, Task>(
                    sp.GetRequiredService<ILogger>(),
                    sp.GetRequiredService<IConnection>(),
                    queueName,
                    prefetchCount,
                    (request) => functionToExecute(sp.GetService<TService>()!, request)
                )
            );
    }

    public static IModel BasicSetup(
        this IModel model,
        string serviceName,
        IDictionary<string, string> routes)
    {
        var exchangeName = $"{serviceName}{EXCHANGE_NAME_SUFFIX}";
        
        model.ExchangeDeclare(
            exchange: exchangeName,
            type: "topic",
            durable: true,
            autoDelete: false);

        foreach (var route in routes)
        {
            var routingKey = route.Key;
            var processName = route.Value;
            var queueName = $"{serviceName}_{processName}{QUEUE_NAME_SUFFIX}";

            model.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            model.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey);
        }

        return model;
    }
}