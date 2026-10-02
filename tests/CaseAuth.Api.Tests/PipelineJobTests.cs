using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaseAuth.Api.Contracts;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Tests;

// Exercises the background pipeline scaffold: PipelineJobsController enqueues a row, and
// PipelineBackgroundService (polling every 1s in tests, see ApiFactory) picks it up and runs it
// through PipelineJobProcessor against the Fixture/Deterministic placeholder implementations.
public class PipelineJobTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient ClientFor(string username)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", username);
        return client;
    }

    private static async Task<(CaseResponse Case, DocumentResponse Document)> CreateCaseWithDocumentAsync(HttpClient analyst)
    {
        var created = await analyst.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Jane Doe", null, null, null));
        var c = (await created.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;

        var pdfPart = new ByteArrayContent("%PDF-1.4 fixture"u8.ToArray());
        pdfPart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        using var fileContent = new MultipartFormDataContent
        {
            { new StringContent("GovernmentId"), "documentType" },
            { pdfPart, "file", "id.pdf" },
        };
        var uploadResponse = await analyst.PostAsync($"/api/cases/{c.Id}/documents", fileContent);
        var document = (await uploadResponse.Content.ReadFromJsonAsync<DocumentResponse>(JsonOptions))!;

        return (c, document);
    }

    private static async Task<PipelineJobResponse> EnqueueAsync(HttpClient client, Guid caseId, PipelineJobType jobType)
    {
        var response = await client.PostAsJsonAsync($"/api/cases/{caseId}/pipeline-jobs",
            new EnqueuePipelineJobRequest(jobType), JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PipelineJobResponse>(JsonOptions))!;
    }

    private static async Task<PipelineJobResponse> WaitForTerminalStateAsync(
        HttpClient client, Guid caseId, Guid jobId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            var jobs = await client.GetFromJsonAsync<List<PipelineJobResponse>>($"/api/cases/{caseId}/pipeline-jobs", JsonOptions);
            var job = jobs!.Single(j => j.Id == jobId);
            if (job.Status is PipelineJobStatus.Completed or PipelineJobStatus.Failed)
            {
                return job;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Pipeline job {jobId} did not reach a terminal state in time.");
    }

    [Fact]
    public async Task EnqueuedExtractJob_IsProcessedInBackground_AndTransitionsCaseToExtracted()
    {
        var analyst = ClientFor("analyst1");
        var (c, _) = await CreateCaseWithDocumentAsync(analyst);

        var enqueued = await EnqueueAsync(analyst, c.Id, PipelineJobType.Extract);
        Assert.Equal(PipelineJobStatus.Pending, enqueued.Status);

        var finished = await WaitForTerminalStateAsync(analyst, c.Id, enqueued.Id);
        Assert.Equal(PipelineJobStatus.Completed, finished.Status);

        var updatedCase = await analyst.GetFromJsonAsync<CaseResponse>($"/api/cases/{c.Id}", JsonOptions);
        Assert.Equal(CaseStatus.Extracted, updatedCase!.Status);

        var auditEvents = await analyst.GetFromJsonAsync<List<AuditEventResponse>>($"/api/cases/{c.Id}/audit-events", JsonOptions);
        Assert.Contains(auditEvents!, e => e.Action == "Pipeline.Extract.Completed");
    }

    [Fact]
    public async Task FullPipeline_RunsExtractScreenAndAiReview_EndingInAiReviewedWithDeterministicFallback()
    {
        var analyst = ClientFor("analyst1");
        var (c, _) = await CreateCaseWithDocumentAsync(analyst);

        var extract = await EnqueueAsync(analyst, c.Id, PipelineJobType.Extract);
        Assert.Equal(PipelineJobStatus.Completed, (await WaitForTerminalStateAsync(analyst, c.Id, extract.Id)).Status);

        var screen = await EnqueueAsync(analyst, c.Id, PipelineJobType.Screen);
        Assert.Equal(PipelineJobStatus.Completed, (await WaitForTerminalStateAsync(analyst, c.Id, screen.Id)).Status);

        var aiReview = await EnqueueAsync(analyst, c.Id, PipelineJobType.AiReview);
        Assert.Equal(PipelineJobStatus.Completed, (await WaitForTerminalStateAsync(analyst, c.Id, aiReview.Id)).Status);

        var updatedCase = await analyst.GetFromJsonAsync<CaseResponse>($"/api/cases/{c.Id}", JsonOptions);
        Assert.Equal(CaseStatus.AiReviewed, updatedCase!.Status);

        // No real IAiReviewer is registered yet - DeterministicAiReviewer's fallback recommends
        // Escalate rather than Approve, so a missing reviewer can never look like a clean approval.
        var reviews = await analyst.GetFromJsonAsync<List<AiReviewResponse>>($"/api/cases/{c.Id}/ai-reviews", JsonOptions);
        Assert.Single(reviews!, r => r.ModelName == "deterministic-fallback" && r.Recommendation == AiRecommendation.Escalate);
    }

    [Fact]
    public async Task EnqueuedJob_WithIllegalFromStatus_EndsUpFailed_WithoutChangingCaseStatus()
    {
        var analyst = ClientFor("analyst1");
        var (c, _) = await CreateCaseWithDocumentAsync(analyst);

        // Case is still Uploaded - Screen requires Extracted first.
        var screen = await EnqueueAsync(analyst, c.Id, PipelineJobType.Screen);
        var finished = await WaitForTerminalStateAsync(analyst, c.Id, screen.Id);

        Assert.Equal(PipelineJobStatus.Failed, finished.Status);
        Assert.NotNull(finished.Error);

        var updatedCase = await analyst.GetFromJsonAsync<CaseResponse>($"/api/cases/{c.Id}", JsonOptions);
        Assert.Equal(CaseStatus.Uploaded, updatedCase!.Status);
    }
}
