namespace CaseAuth.Api.Infrastructure;

// Accepts a caller-supplied X-Correlation-Id (useful for tying a client-side request to its
// audit event) or generates one, echoes it back on the response, and stashes it in
// HttpContext.Items for AuditService and ApiExceptionMiddleware to read.
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }
}
