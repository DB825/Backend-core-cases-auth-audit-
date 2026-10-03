namespace CaseAuth.Api.Pipeline;

public class AiReviewAgentOptions
{
    public const string SectionName = "AiReviewAgent";

    // "Remote" calls the real AI Review Agent service. "Deterministic" keeps the old
    // escalate-only stub - a labeled fallback for demoing without that service running.
    public string Mode { get; set; } = "Remote";

    public string BaseUrl { get; set; } = "http://localhost:5176";

    public int TimeoutSeconds { get; set; } = 30;
}
