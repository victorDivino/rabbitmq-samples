using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQSamples.Application.Common.Commands;
using RabbitMQSamples.Application.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRabbitMQ(builder.Configuration, c => c.Build());

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

app.MapPost("/message",([FromBody] HelloCommand command, [FromServices] IModel model) =>
{
    IBasicProperties basicProperties = model.CreateBasicProperties();
    basicProperties.Persistent = true;
    basicProperties.MessageId = Guid.NewGuid().ToString("D");

    var json = JsonSerializer.Serialize(command, new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    var data = Encoding.UTF8.GetBytes(json);

    model.BasicPublish(
        "hello_service", 
        "Hello.Message", 
        true,
        basicProperties,
        data);
    
})
.WithName("message");

#endregion

app.Run();


