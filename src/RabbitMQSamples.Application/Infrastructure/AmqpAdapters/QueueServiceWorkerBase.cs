using Microsoft.Extensions.Hosting;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQSamples.Application.Infrastructure.AmqpAdapters;

public abstract class QueueServiceWorkerBase: BackgroundService
{
    protected readonly ILogger Logger;
    protected readonly IConnection Connection;
    protected IModel? Model { get; private set; }
    public ushort PrefetchCount { get; }
    public string QueueName { get; }
    
    protected QueueServiceWorkerBase(
        ILogger logger,
        IConnection connection,
        ushort prefetchCount,
        string queueName)
    {
        Logger = logger;
        Connection = connection;
        PrefetchCount = prefetchCount;
        QueueName = queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Model = BuildModel();
        IBasicConsumer consumer = BuildConsumer();
        WaitQueueCreation();

        string consumerTag = consumer.Model.BasicConsume(
                         queue: QueueName,
                         autoAck: false,
                         consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            Logger.Information("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }

        Model.BasicCancelNoWait(consumerTag);
    }

     protected virtual void WaitQueueCreation()
    {
        Policy
        .Handle<OperationInterruptedException>()
            .WaitAndRetry(5, retryAttempt =>
            {
                var timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                
                Logger.Warning(
                    "Queue {queueName} not found... We will try in {tempo} ms.",
                    QueueName,
                    timeToWait.TotalMilliseconds);
                
                return timeToWait;
            })
            .Execute(() =>
            {
                using IModel testModel = this.BuildModel();
                testModel.QueueDeclarePassive(QueueName);
            });
    }

    protected virtual IModel BuildModel()
    {
        IModel model = Connection.CreateModel();
        model.BasicQos(0, PrefetchCount, false);
        return model;
    }

    protected abstract IBasicConsumer BuildConsumer();
}