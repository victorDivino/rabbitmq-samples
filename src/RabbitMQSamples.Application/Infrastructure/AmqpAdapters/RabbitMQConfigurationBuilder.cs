using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQSamples.Application.Infrastructure.AmqpAdapters;

public sealed class RabbitMQConfigurationBuilder
{
    private readonly IServiceCollection Services;
    private IConfiguration Configuration;
    private string ConfigurationPrefix = "RabbitMqConnection";
    private int ConnectMaxAttempts = 8;
    private Func<int, TimeSpan> ProduceWaitConnectWait = (retryAttempt) => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

    public RabbitMQConfigurationBuilder(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public void Build()
    {
        ConfigureConnectionFactory();
        ConfigureCreateConnection();
        ConfigureCreateModel();
    }

    private void ConfigureConnectionFactory()
    {
        Services.AddSingleton(sp =>
        {
            ConnectionFactory factory = new();
            Configuration.Bind(ConfigurationPrefix, factory);
            return factory;
        });
    }

    private void ConfigureCreateConnection()
    {
        Services.AddSingleton(sp => Policy
             .Handle<BrokerUnreachableException>()
             .WaitAndRetry(ConnectMaxAttempts, retryAttempt =>
             {
                 TimeSpan wait = ProduceWaitConnectWait(retryAttempt);
                 Console.WriteLine($"Can't create a connection with RabbitMQ. We wil try again in {wait.TotalMilliseconds}.");
                 return wait;
             })
             .Execute(() =>
             {
                 System.Diagnostics.Debug.WriteLine("Trying to create a connection with RabbitMQ");

                 IConnection connection = sp.GetRequiredService<ConnectionFactory>().CreateConnection();

                 Console.WriteLine(@$"Connected on RabbitMQ '{connection}' with name '{connection.ClientProvidedName}'. 
....Local Port: {connection.LocalPort}
....Remote Port: {connection.RemotePort}");

                 return connection;
             })
     );
    }

    private void ConfigureCreateModel()
        => Services.AddTransient(sp => sp.GetRequiredService<IConnection>().CreateModel());
}
