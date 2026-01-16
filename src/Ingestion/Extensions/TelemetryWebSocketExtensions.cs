using Ingestion.Infrastructure.Networking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Ingestion.Extensions
{
    public static class TelemetryWebSocketExtensions
    {
        private const string TelemetryWebSocketPath = "/ws/telemetry";

        public static IServiceCollection AddTelemetryWebSocket(this IServiceCollection services)
        {
            services.AddSingleton<TelemetryWebSocketHandler>();

            return services;
        }

        public static WebApplication MapTelemetryWebSocket(this WebApplication app)
        {
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            app.Map(TelemetryWebSocketPath, async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var handler = context.RequestServices.GetRequiredService<TelemetryWebSocketHandler>();
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await handler.HandleAsync(socket, context.RequestAborted);
            });

            return app;
        }
    }
}
