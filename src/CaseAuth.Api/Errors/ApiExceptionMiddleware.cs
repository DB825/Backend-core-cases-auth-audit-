using CaseAuth.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Errors;

// Translates domain exceptions (and EF's concurrency exception) into a consistent
// ProblemDetails body carrying the request's correlation id, and keeps stack traces/PII out
// of the response for anything unexpected.
public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id)
                ? id as string
                : null;

            var (statusCode, title) = ex switch
            {
                ApiException api => (api.StatusCode, api.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                    "The resource was modified by another request. Reload and retry."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(ex, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Extensions = { ["correlationId"] = correlationId },
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
