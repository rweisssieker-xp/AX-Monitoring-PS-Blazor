namespace AXMonitoringBU.Api.Middleware;

public class BufferResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BufferResponseMiddleware> _logger;

    public BufferResponseMiddleware(RequestDelegate next, ILogger<BufferResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream to buffer the response
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            // Call the next middleware
            await _next(context);

            // Set Content-Length header to avoid chunked encoding
            memoryStream.Position = 0;
            context.Response.ContentLength = memoryStream.Length;

            // Copy the buffered response to the original stream
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BufferResponseMiddleware");
            throw;
        }
        finally
        {
            // Restore original response body stream
            context.Response.Body = originalBodyStream;
        }
    }
}
