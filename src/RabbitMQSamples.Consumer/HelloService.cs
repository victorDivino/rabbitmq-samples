using RabbitMQSamples.Application.Common.Commands;

namespace RabbitMQSamples.Consumer;

public sealed class HelloService 
{
    private readonly ILogger _logger;

    public HelloService(ILogger logger) 
        => _logger = logger;
    
    public Task Handle(HelloCommand command)
    {
        _logger.Information($"{nameof(HelloService)} {{@Command}}", command);
        return Task.CompletedTask;
    }
}