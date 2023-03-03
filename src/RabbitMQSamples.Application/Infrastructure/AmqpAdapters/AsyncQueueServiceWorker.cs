using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQSamples.Application.Common.Enums;

namespace RabbitMQSamples.Application.Infrastructure.AmqpAdapters;

public class AsyncQueueServiceWorker<TRequest, TResponse> : QueueServiceWorkerBase
    where TResponse : Task
    where TRequest : struct
{
    protected readonly Func<TRequest, TResponse> DispatchFunc;

    public AsyncQueueServiceWorker(
        ILogger logger,
        IConnection connection,
        string queueName,
        ushort prefetchCount,
        Func<TRequest, TResponse> dispatchFunc)
        : base(
            logger,
            connection,
            prefetchCount,
            queueName)
            => DispatchFunc = dispatchFunc;

    protected override IBasicConsumer BuildConsumer()
    {
        AsyncEventingBasicConsumer consumer = new(Model);
        consumer.Received += Receive;
        return consumer;
    }

    private async Task Receive(object sender, BasicDeliverEventArgs receivedItem)
    {
        if (receivedItem is null)
            throw new ArgumentNullException(nameof(receivedItem));

        if (receivedItem.BasicProperties is null)
            throw new ArgumentNullException(nameof(receivedItem.BasicProperties));

        PostConsumeAction postReceiveAction = TryDeserialize(receivedItem, out TRequest request);

        if (postReceiveAction == PostConsumeAction.None)
        {
            try
            {
                postReceiveAction = await this.Dispatch(receivedItem, request);
            }
            catch (Exception exception)
            {
                postReceiveAction = PostConsumeAction.Nack;

                Logger.Warning(
                    "Exception on processing message {queueName} {exception}",
                    this.QueueName,
                    exception);
            }
        }

        switch (postReceiveAction)
        {
            case PostConsumeAction.None: throw new InvalidOperationException("None is unsupported");
            case PostConsumeAction.Ack: Model!.BasicAck(receivedItem.DeliveryTag, false); break;
            case PostConsumeAction.Nack: Model!.BasicNack(receivedItem.DeliveryTag, false, false); break;
            case PostConsumeAction.Reject: Model!.BasicReject(receivedItem.DeliveryTag, false); break;
        }
    }

    private PostConsumeAction TryDeserialize(BasicDeliverEventArgs receivedItem, out TRequest request)
    {
        if (receivedItem is null)
            throw new ArgumentNullException(nameof(receivedItem));

        var postReceiveAction = PostConsumeAction.None;
        request = default;

        try
        {
            var message = Encoding.UTF8.GetString(receivedItem.Body.ToArray());
            request = JsonSerializer.Deserialize<TRequest>(message, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception exception)
        {
            postReceiveAction = PostConsumeAction.Reject;
            Logger.Error("Message rejected during deserialization {exception}", exception);
        }

        return postReceiveAction;
    }

    protected virtual async Task<PostConsumeAction> Dispatch(BasicDeliverEventArgs receivedItem, TRequest request)
    {
        if (receivedItem is null)
            throw new ArgumentNullException(nameof(receivedItem));

        await this.DispatchFunc(request);

        return PostConsumeAction.Ack;
    }
}