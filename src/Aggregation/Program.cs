using Aggregation;
using Aggregation.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAkkaService();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
