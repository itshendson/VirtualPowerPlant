using Microsoft.AspNetCore.Http;

namespace Ingestion.Handlers;

/// <summary>
/// Represents the result of a command operation.
/// Provides a clean way to return success/failure with appropriate HTTP status codes.
/// </summary>
public record CommandResult : ICommandResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int HttpStatusCode { get; init; }

    private CommandResult(bool isSuccess, string? errorMessage, int httpStatusCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Creates a successful result with 202 Accepted status (appropriate for async operations).
    /// </summary>
    public static CommandResult Success() => new(true, null, StatusCodes.Status202Accepted);

    /// <summary>
    /// Creates a failure result with 500 Internal Server Error status.
    /// </summary>
    public static CommandResult Failure(string? errorMessage = null) 
        => new(false, errorMessage, StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Creates a failure result with a custom HTTP status code.
    /// </summary>
    public static CommandResult Failure(int httpStatusCode, string? errorMessage = null) 
        => new(false, errorMessage, httpStatusCode);
}

