namespace AXMonitoringBU.Api.Middleware;

public class ForceContentLengthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ForceContentLengthMiddleware> _logger;

    public ForceContentLengthMiddleware(RequestDelegate next, ILogger<ForceContentLengthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only buffer JSON responses
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var originalBodyStream = context.Response.Body;

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context);

                // Copy the response to memory, then write it with Content-Length
                memoryStream.Position = 0;
                var content = memoryStream.ToArray();

                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = content.Length;

                await context.Response.Body.WriteAsync(content, 0, content.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForceContentLengthMiddleware");
                context.Response.Body = originalBodyStream;
                throw;
            }
        }
        else
        {
            await _next(context);
        }
    }
}
