using Ingestion.Extensions;
using Ingestion.Handlers;
using Ingestion.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ingestion.Controllers
{
    [ApiController]
    [Route("api/v1/telemetry")]
    public class TelemetryController : ControllerBase
    {
        private readonly IMediator _mediatr;

        public TelemetryController(IMediator mediatr)
        {
            _mediatr = mediatr;
        }

        [HttpPost(Name = "IngestTelemetry")]
        public async Task<ActionResult> IngestTelemetry([FromBody] TelemetryReadingRequest request, CancellationToken cancellationToken)
        {
            var command = new IngestTelemetryCommand(request);
            var result = await _mediatr.Send(command, cancellationToken);
            return result.ToActionResult();
        }
    }
}
