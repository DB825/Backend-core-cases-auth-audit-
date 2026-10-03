namespace CaseAuth.Api.Pipeline;

public interface IPipelineJobProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken ct);
}
