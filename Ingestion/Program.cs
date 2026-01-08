using Ingestion.Extensions;
using Ingestion.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediator();
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddTelemetryIngestBuffer(builder.Configuration);

builder.Services.AddHostedService<TelemetryIngestBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
