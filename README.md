# VirtualPowerPlant

Real-time telemetry platform that streams battery data, aggregates it into a digital twin, and visualizes live fleet performance.

![TelemetrySender console generating random telemetry while the Metrics Dashboard updates live](docs/VPP-Showcase.gif)

*TelemetrySender streams random telemetry over WebSockets while the live dashboard updates in real time. My core focus was the ingestion and aggregation services; TelemetrySender and the dashboard UI were generated to keep the emphasis on backend work.*

## Features
- **High-throughput ingestion** over WebSockets with binary protobuf telemetry.
- **Backpressure-friendly buffering** via `ITelemetryIngestBuffer` to decouple ingest from Kafka.
- **Event-driven aggregation** using Akka.NET Cluster + Sharding for region/substation/site rollups.
- **Live dashboard UX** with near-real-time metrics, trends, and animated sparklines.

## Architecture (high level)
The system is split into focused services so ingestion, aggregation, and visualization scale independently.

1. TelemetrySender simulates edge devices, sending protobuf telemetry over WebSockets.
2. Ingestion accepts WebSocket telemetry, validates it, buffers it, and publishes to Kafka.
3. Aggregation consumes Kafka events, updates the Akka.NET digital twin, and maintains aggregate snapshots.
4. MetricsDashboard serves a lightweight web UI and exposes `/api/metrics/*` for the live dashboard.

```
TelemetrySender
    │  WebSocket (protobuf)
    ▼
Ingestion ──► Buffer ──► Kafka ──► Aggregation (Akka.NET cluster)
                                          │
                                          ▼
                                 Metrics API + Dashboard
```

## Limitations (production considerations)
- The current ingest buffer is in-memory; production would use a durable queue or datastore (e.g., Redis) to avoid data loss and enable replay.
- Backpressure is simplified; a real deployment would apply reactive backpressure to protect Kafka and downstream consumers.
- Observability is minimal; production would include structured logs, tracing, and metrics with alerting.
