namespace CaseAuth.Api.Infrastructure;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}

public class CorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    public string CorrelationId =>
        accessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string
        ?? throw new InvalidOperationException("No correlation id is available on the current request.");
}
