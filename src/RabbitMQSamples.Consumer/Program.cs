using RabbitMQSamples.Application.Common.Commands;
using RabbitMQSamples.Application.Common.Extensions;
using Serilog;

namespace RabbitMQSamples.Consumer;

public static class Program
{
    public static void Main(string[] args)
        => CreateHostBuilder(args)
            .Build()
            .Run();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .UseSerilog((ctx, lc) => 
                lc.MinimumLevel.Debug()
                .WriteTo.Console())
            .ConfigureServices(Configure);

    private static void Configure(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddTransient<HelloService>();

        services.AddRabbitMQ(hostContext.Configuration, c => c.Build());

        services.AddHostedService<SetupWorker>();

        services.MapQueue<HelloService, HelloCommand>("hello_default_queue", 3, (svc, data) => svc.Handle(data));
    }
}
