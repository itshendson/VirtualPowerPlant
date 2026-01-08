public interface ICommandResult
{
    bool IsSuccess { get; }
    string? ErrorMessage { get; }
    int HttpStatusCode { get; }
}