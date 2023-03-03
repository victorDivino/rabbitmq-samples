using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQSamples.Application.Infrastructure.AmqpAdapters;

namespace RabbitMQSamples.Application.Common.Extensions;

public static class RabbitMQDependencyInjectionExtensions
{
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RabbitMQConfigurationBuilder> action)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        if (action is null)
            throw new ArgumentNullException(nameof(action));

        RabbitMQConfigurationBuilder builder = new(services, configuration);
        action(builder);
        builder.Build();

        return services;
    }
}