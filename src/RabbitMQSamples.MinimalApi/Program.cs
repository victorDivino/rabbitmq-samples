using System.Text;
using Microsoft.AspNetCore.Mvc;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQSamples.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);

#region RabbitMQ

var factory = new ConnectionFactory()
{
    HostName = "localhost",
    UserName = "user",
    Password = "user",
    VirtualHost = "local",
    Port = 5672,
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
};

var policy = Policy.Handle<BrokerUnreachableException>()
    .WaitAndRetry(new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3),
    });

builder.Services.AddTransient(sp => sp.GetRequiredService<IConnection>().CreateModel());

builder.Services.AddSingleton(sp =>
       {
           ConnectionFactory factory = new();
           builder.Configuration.Bind("RABBITMQ", factory);
           return factory;
       });

builder.Services.AddSingleton(
    sp => policy.Execute(
        () => sp.GetRequiredService<ConnectionFactory>().CreateConnection()));

#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region Middleware

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

#endregion

#region Endpoints

app.MapPost("/message",([FromBody] HelloRequest input, [FromServices] IModel model) =>
{
    IBasicProperties basicProperties = model.CreateBasicProperties();
    basicProperties.MessageId = Guid.NewGuid().ToString("D");

    var json = System.Text.Json.JsonSerializer.Serialize(input);
    var data = Encoding.UTF8.GetBytes(json);

    model.BasicPublish(
        "hello_exchange", 
        string.Empty, 
        true,
        basicProperties,
        data);
    
})
.WithName("message");

#endregion

app.Run();


