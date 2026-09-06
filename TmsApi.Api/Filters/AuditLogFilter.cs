using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Filters;

public class AuditLogFilter(ILogger<AuditLogFilter> logger) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var route = context.HttpContext.Request.Path;
        var Method = context.HttpContext.Request.Method;
        logger.LogInformation("TMS API call: {Method} {Route}", Method, route);
    }
    public void OnActionExecuted(ActionExecutedContext context)
    {
        var status = context.HttpContext.Response.StatusCode;
        logger.LogInformation("TMS API response: {StatusCode}", status);
    }
}