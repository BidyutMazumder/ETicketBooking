namespace Booking.API.Middleware;

/// <summary>
/// Middleware for comprehensive audit logging of all requests and responses
/// </summary>
public sealed class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract user ID if available
        var userId = context.Items.TryGetValue("UserId", out var uid) ? uid?.ToString() : "anonymous";
        var requestId = context.TraceIdentifier;
        var timestamp = DateTime.UtcNow;

        _logger.LogInformation(
            "Audit: Incoming request [RequestId={RequestId}] [User={UserId}] [Method={Method}] [Path={Path}] [Timestamp={Timestamp}]",
            requestId, userId, context.Request.Method, context.Request.Path, timestamp);

        // Capture response details
        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                var statusCode = context.Response.StatusCode;
                var duration = (DateTime.UtcNow - timestamp).TotalMilliseconds;

                _logger.LogInformation(
                    "Audit: Response [RequestId={RequestId}] [User={UserId}] [StatusCode={StatusCode}] [DurationMs={DurationMs}]",
                    requestId, userId, statusCode, duration);

                // Copy response to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Audit: Exception [RequestId={RequestId}] [User={UserId}] [Exception={Exception}]",
                    requestId, userId, ex.GetType().Name);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
