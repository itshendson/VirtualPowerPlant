using MetricsDashboard.Extensions;
using MetricsDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAggregateSnapshotStore, InMemoryAggregateSnapshotStore>();
builder.Services.AddKafka(builder.Configuration);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/metrics/sites", (IAggregateSnapshotStore store) => Results.Ok(store.GetSites()));
app.MapGet("/api/metrics/substations", (IAggregateSnapshotStore store) => Results.Ok(store.GetSubstations()));
app.MapGet("/api/metrics/regions", (IAggregateSnapshotStore store) => Results.Ok(store.GetRegions()));

app.Run();
