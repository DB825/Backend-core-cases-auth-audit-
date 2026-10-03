namespace CaseAuth.Api.Pipeline;

public class PipelineOptions
{
    public const string SectionName = "Pipeline";

    public int PollIntervalSeconds { get; set; } = 2;
    public int MaxJobsPerPoll { get; set; } = 10;
}
