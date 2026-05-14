namespace Booking.API.Middleware;

/// <summary>
/// Middleware for JWT token validation and user context extraction
/// </summary>
public sealed class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromHeader(context);

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Extract user ID from token claims
                var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("userid");
                
                if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    context.Items["UserId"] = userId;
                    _logger.LogDebug("JWT validated for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting user context from JWT");
            }
        }

        await _next(context);
    }

    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        const string scheme = "Bearer ";
        var header = context.Request.Headers.Authorization.FirstOrDefault();

        if (header?.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) == true)
        {
            return header.Substring(scheme.Length);
        }

        return null;
    }
}
