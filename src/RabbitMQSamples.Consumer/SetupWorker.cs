using RabbitMQ.Client;
using RabbitMQSamples.Application.Common.Extensions;

namespace RabbitMQSamples.Consumer;


public sealed class SetupWorker : BackgroundService
{
    private readonly ILogger Logger;
    private readonly IModel Model;

    public SetupWorker(ILogger logger, IModel model)
    {
        Logger = logger;
        Model = model;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        Model.BasicSetup("hello", new Dictionary<string, string>
        {
            { "*.*", "default" }
        });
    }
}