using System.ComponentModel.DataAnnotations;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

public record EnqueuePipelineJobRequest([Required] PipelineJobType JobType);

public record PipelineJobResponse(
    Guid Id,
    Guid CaseId,
    PipelineJobType JobType,
    PipelineJobStatus Status,
    int Attempts,
    string? Error,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt)
{
    public static PipelineJobResponse From(ProcessingJob j) => new(
        j.Id, j.CaseId, j.JobType, j.Status, j.Attempts, j.Error, j.CreatedAt, j.StartedAt, j.CompletedAt);
}
