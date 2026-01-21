using Aggregation.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAkkaService();
builder.Services.AddKafka(builder.Configuration);
var host = builder.Build();
host.Run();
