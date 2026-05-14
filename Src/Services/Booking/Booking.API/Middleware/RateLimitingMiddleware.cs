namespace Booking.API.Middleware;

/// <summary>
/// Middleware for rate limiting to prevent abuse
/// Implements per-user and per-IP rate limiting
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, RateLimitEntry> RateLimitCache = new();
    private const int MaxRequestsPerMinute = 100;
    private const int MaxHoldsPerMinute = 10;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = GetRateLimitKey(context);
        
        if (!IsRateLimited(context.Request.Path, key))
        {
            await _next(context);
        }
        else
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new { error = "Too many requests. Please try again later." };
            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static string GetRateLimitKey(HttpContext context)
    {
        // Use UserId if available (authenticated), otherwise use IP address
        if (context.Items.TryGetValue("UserId", out var userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    private static bool IsRateLimited(PathString path, string key)
    {
        var now = DateTime.UtcNow;
        var limit = path.Value?.Contains("/reservations/hold") == true ? MaxHoldsPerMinute : MaxRequestsPerMinute;

        lock (RateLimitCache)
        {
            if (RateLimitCache.TryGetValue(key, out var entry))
            {
                if ((now - entry.ResetTime).TotalSeconds > 60)
                {
                    // Reset the counter after 1 minute
                    entry.Count = 1;
                    entry.ResetTime = now;
                    return false;
                }

                entry.Count++;
                return entry.Count > limit;
            }
            else
            {
                RateLimitCache[key] = new RateLimitEntry { Count = 1, ResetTime = now };
                
                // Clean up old entries
                var expiredKeys = RateLimitCache
                    .Where(x => (now - x.Value.ResetTime).TotalMinutes > 2)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var expiredKey in expiredKeys)
                {
                    RateLimitCache.Remove(expiredKey);
                }

                return false;
            }
        }
    }

    private sealed class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
