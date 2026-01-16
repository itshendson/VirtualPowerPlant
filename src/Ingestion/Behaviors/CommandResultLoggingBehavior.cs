using Ingestion.Handlers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ingestion.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically logs CommandResult responses.
/// This removes the need for controllers to manually log success/failure.
/// </summary>
public class CommandResultLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ICommandResult
{
    private readonly ILogger<CommandResultLoggingBehavior<TRequest, TResponse>> _logger;

    public CommandResultLoggingBehavior(ILogger<CommandResultLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var result = await next();

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Command {CommandType} completed successfully. StatusCode: {StatusCode}",
                typeof(TRequest).Name,
            result.HttpStatusCode);
        } 
        else
        {
            _logger.LogWarning(
                "Command {CommandType} failed. StatusCode: {StatusCode}, Error: {Error}",
                typeof(TRequest).Name,
            result.HttpStatusCode,
            result.ErrorMessage);
        }

        return result;
    }
}

