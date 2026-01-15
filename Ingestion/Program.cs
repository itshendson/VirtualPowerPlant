using Ingestion.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator();
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddTelemetryIngestBuffer(builder.Configuration);
builder.Services.AddTelemetryIngestBackgroundService(builder.Configuration);
builder.Services.AddTelemetryWebSocket();

var app = builder.Build();

app.MapTelemetryWebSocket();

app.Run();
