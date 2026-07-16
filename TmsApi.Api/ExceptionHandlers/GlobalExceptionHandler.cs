using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace TmsApi.ExceptionHandlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };
        if (exception is ValidationException validationException)
        {
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Validation Failed";
            problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
            problemDetails.Detail = "One or more validation errors occurred.";
            problemDetails.Extensions["errors"]= validationException.Errors
            .GroupBy(e=>e.PropertyName)
            .ToDictionary(
                g=>g.Key,
                g=>g.Select(e =>e.ErrorMessage).ToArray()
            );
        }
        else
        {
          problemDetails.Status = (int)HttpStatusCode.InternalServerError;
          problemDetails.Title= "An unexpected error occurred";
          problemDetails.Type= "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1";
          problemDetails.Detail= "Internal server error. Please trace using the provided correlation ID";  
        }
        var correlationId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
    
}