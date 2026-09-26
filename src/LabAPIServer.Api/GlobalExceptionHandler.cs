using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace LabAPIServer.Api;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var isDatabaseFailure = exception is SqlException;
        httpContext.Response.StatusCode = isDatabaseFailure
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = isDatabaseFailure ? "The database is currently unavailable." : "An unexpected error occurred.",
            Type = isDatabaseFailure ? "https://httpstatuses.com/503" : "https://httpstatuses.com/500"
        };

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
