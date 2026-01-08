using Ingestion.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Ingestion.Extensions;

/// <summary>
/// Extension methods for controllers to simplify CommandResult handling.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Converts a CommandResult to an appropriate ActionResult.
    /// Handles success and failure cases with proper HTTP status codes.
    /// </summary>
    public static ActionResult ToActionResult(this CommandResult result)
    {
        if (result.IsSuccess)
        {
            return new StatusCodeResult(result.HttpStatusCode);
        }

        return new ObjectResult(result.ErrorMessage)
        {
            StatusCode = result.HttpStatusCode
        };
    }
}

