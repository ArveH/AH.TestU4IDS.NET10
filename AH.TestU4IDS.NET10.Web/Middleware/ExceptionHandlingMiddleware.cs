namespace AH.TestU4IDS.NET10.Web.Middleware;

using AH.TestU4IDS.NET10.Web.Services;

/// <summary>
/// Middleware for handling unhandled exceptions and storing them in the ErrorService.
/// This catches connection errors, configuration errors, and other infrastructure issues
/// that occur during OIDC authentication flows.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ErrorService errorService)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred during request processing");

            // Extract a user-friendly error message
            var errorMessage = ExtractErrorMessage(ex);

            // Also store in the service for same-scope consumers.
            errorService.SetError(errorMessage);

            // Redirect to home page and carry the error in the query string so it
            // survives the redirect into a new request/DI scope and static SSR render.
            if (!context.Response.HasStarted)
            {
                var encoded = Uri.EscapeDataString(errorMessage);
                context.Response.Redirect($"/?authError={encoded}");
            }
        }
    }

    /// <summary>
    /// Extracts a user-friendly error message from exceptions, unwrapping nested exceptions as needed.
    /// </summary>
    private static string ExtractErrorMessage(Exception ex)
    {
        // Handle specific exception types and messages
        var message = ex.Message;

        return message switch
        {
            // Connection errors during OIDC configuration retrieval
            _ when message.Contains("No connection could be made") =>
                "Unable to connect to the identity server. Please ensure the authentication service is running.",
            _ when message.Contains("actively refused it") =>
                "Connection refused by the authentication server. Please check if the identity server is running.",
            _ when message.Contains("IDX20804") || message.Contains("Unable to retrieve document") =>
                "Unable to retrieve authentication configuration. Please verify your identity server URL and that the server is accessible.",
            _ when message.Contains("IDX20803") || message.Contains("Unable to obtain configuration") =>
                "Unable to obtain authentication configuration. The identity server may be unavailable or unreachable.",
            _ when message.Contains("SocketException") =>
                "Network error: Unable to reach the authentication server. Please verify your network connection and server configuration.",
            // Check inner exception if the current message doesn't match
            _ when ex.InnerException != null =>
                ExtractErrorMessage(ex.InnerException),
            // Default fallback
            _ => $"An authentication error occurred: {ex.Message}"
        };
    }
}
