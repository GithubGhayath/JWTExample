using System.Net;
using Microsoft.AspNetCore.Diagnostics;

public class GlobalExceptionHandler : IExceptionHandler
{
    //  Logger used to record exception details for debugging or monitoring.
    private readonly ILogger<GlobalExceptionHandler> _logger;

    //  Dependency Injection constructor — logger is injected automatically.
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this._logger = logger;
    }

    //  This method is called automatically when an unhandled exception occurs during a request.
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        //  Get user-friendly error details (HTTP status + message) based on exception type.
        var Exception = GetExceptionDetails(exception);

        //  Log full details (stack trace, message, etc.) to help diagnose issues.
        _logger.LogError(exception, exception.Message);

        //  Set the correct HTTP status code (like 401, 400, 500...).
        httpContext.Response.StatusCode = (int)Exception.statusCode;

        //  Send a simple JSON message back to the client describing the error.
        await httpContext.Response.WriteAsJsonAsync(Exception.message, cancellationToken);

        //  Return true → means we’ve handled the exception, so no further middleware will process it.
        return true;
    }

    //  Maps known exception types to specific HTTP status codes and messages.
    private (HttpStatusCode statusCode, string message) GetExceptionDetails(Exception exception)
    {
        //  Pattern matching on exception type:
        return exception switch
        {
            LoginFailedException => (HttpStatusCode.Unauthorized, exception.Message),   // 401
            UserAlreadyExistsException => (HttpStatusCode.Conflict, exception.Message), // 409
            RegistrationFaildException => (HttpStatusCode.BadRequest, exception.Message), // 400
            RefreshTokenException => (HttpStatusCode.Unauthorized, exception.Message),  // 401
            _ => (HttpStatusCode.InternalServerError, 
                  $"An unexpected error occurred: {exception.Message}") // 500 fallback
        };
    }
}
