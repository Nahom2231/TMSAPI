using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
      string correlationId = Guid.NewGuid().ToString("N")[..8];

      context.Response.Headers["X-Correlation-Id"]= correlationId;
       var stopwatch = Stopwatch.StartNew();
       _logger.LogInformation("HTTP {Method} {Path} started.[Correlation ID:{CorrelationId}] ",
       context.Request.Method,
       context.Request.Path,
       correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

         _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms.{CorrelationId}]",
         context.Request.Method,
         context.Request.Path,
         context.Response.StatusCode,
         stopwatch.ElapsedMilliseconds,
         correlationId);
        }
     }
}