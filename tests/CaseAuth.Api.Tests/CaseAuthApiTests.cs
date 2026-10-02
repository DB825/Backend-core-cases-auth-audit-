using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaseAuth.Api.Contracts;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Tests;

public class CaseAuthApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // Based on JsonSerializerDefaults.Web (camelCase + case-insensitive property matching,
    // which ReadFromJsonAsync/GetFromJsonAsync otherwise apply implicitly but lose once you
    // supply any custom options) plus a string enum converter to match Program.cs's
    // AddJsonOptions. Without PropertyNameCaseInsensitive, every response silently
    // deserializes to all-default values instead of throwing, since these are records.
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

    private static async Task<CaseResponse> CreateCaseReadyForDecisionAsync(HttpClient analyst)
    {
        var created = await analyst.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Jane Doe", null, null, null));
        var c = (await created.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;

        await analyst.PostAsync($"/api/cases/{c.Id}/submit", null);
        await analyst.PostAsync($"/api/cases/{c.Id}/start-review", null);
        await analyst.PostAsJsonAsync($"/api/cases/{c.Id}/ai-reviews",
            new CreateAiReviewRequest("demo-classifier", "1.0", AiRecommendation.Approve, "Looks fine."));
        var afterRequest = await analyst.PostAsync($"/api/cases/{c.Id}/request-decision", null);
        return (await afterRequest.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task MissingDevUserHeader_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownDevUser_Returns401()
    {
        var response = await ClientFor("nobody").GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CaseFromOneFirm_IsHiddenFromAnotherFirm()
    {
        var analyst1 = ClientFor("analyst1");
        var analyst2 = ClientFor("analyst2");

        var created = await analyst1.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Jane Doe", null, null, null));
        var c = (await created.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;

        var crossFirmGet = await analyst2.GetAsync($"/api/cases/{c.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossFirmGet.StatusCode);

        var otherFirmList = await analyst2.GetFromJsonAsync<List<CaseResponse>>("/api/cases", JsonOptions);
        Assert.DoesNotContain(otherFirmList!, x => x.Id == c.Id);
    }

    [Fact]
    public async Task RequestDecision_WithoutAnyAiReview_Returns400()
    {
        var analyst = ClientFor("analyst1");
        var created = await analyst.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Jane Doe", null, null, null));
        var c = (await created.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;
        await analyst.PostAsync($"/api/cases/{c.Id}/submit", null);
        await analyst.PostAsync($"/api/cases/{c.Id}/start-review", null);

        var response = await analyst.PostAsync($"/api/cases/{c.Id}/request-decision", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalystCannotApproveDecision()
    {
        var analyst = ClientFor("analyst1");
        var pending = await CreateCaseReadyForDecisionAsync(analyst);

        var response = await SendDecisionAsync(
            analyst, pending.Id, DecisionOutcome.Approved, pending.RowVersion, "analyst-attempt");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Decision_HappyPath_ApprovesCaseAndWritesOneAuditEvent()
    {
        var analyst = ClientFor("analyst1");
        var supervisor = ClientFor("supervisor");
        var pending = await CreateCaseReadyForDecisionAsync(analyst);

        var response = await SendDecisionAsync(
            supervisor, pending.Id, DecisionOutcome.Approved, pending.RowVersion, "key-1");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var updatedCase = await analyst.GetFromJsonAsync<CaseResponse>($"/api/cases/{pending.Id}", JsonOptions);
        Assert.Equal(CaseStatus.Approved, updatedCase!.Status);

        var auditEvents = await analyst.GetFromJsonAsync<List<AuditEventResponse>>($"/api/cases/{pending.Id}/audit-events", JsonOptions);
        Assert.Single(auditEvents!, e => e.Action == "Decision.Approved");
    }

    [Fact]
    public async Task Decision_DuplicateIdempotencyKey_ReplaysResult_WithoutSecondAuditEvent()
    {
        var analyst = ClientFor("analyst1");
        var supervisor = ClientFor("supervisor");
        var pending = await CreateCaseReadyForDecisionAsync(analyst);

        var first = await SendDecisionAsync(supervisor, pending.Id, DecisionOutcome.Approved, pending.RowVersion, "dup-key");
        var firstDecision = (await first.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions))!;

        var second = await SendDecisionAsync(supervisor, pending.Id, DecisionOutcome.Approved, pending.RowVersion, "dup-key");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondDecision = (await second.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions))!;
        Assert.Equal(firstDecision.Id, secondDecision.Id);

        var decisions = await analyst.GetFromJsonAsync<List<DecisionResponse>>($"/api/cases/{pending.Id}/decisions", JsonOptions);
        Assert.Single(decisions!);

        var auditEvents = await analyst.GetFromJsonAsync<List<AuditEventResponse>>($"/api/cases/{pending.Id}/audit-events", JsonOptions);
        Assert.Single(auditEvents!, e => e.Action == "Decision.Approved");
    }

    [Fact]
    public async Task Decision_StaleRowVersion_IsRejectedWithConflict()
    {
        var analyst = ClientFor("analyst1");
        var supervisor = ClientFor("supervisor");
        var pending = await CreateCaseReadyForDecisionAsync(analyst);

        var staleRowVersion = Guid.NewGuid();
        var response = await SendDecisionAsync(
            supervisor, pending.Id, DecisionOutcome.Approved, staleRowVersion, "stale-key");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var decisions = await analyst.GetFromJsonAsync<List<DecisionResponse>>($"/api/cases/{pending.Id}/decisions", JsonOptions);
        Assert.Empty(decisions!);
    }

    private static Task<HttpResponseMessage> SendDecisionAsync(
        HttpClient client, Guid caseId, DecisionOutcome outcome, Guid rowVersion, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/cases/{caseId}/decisions")
        {
            Content = JsonContent.Create(new CreateDecisionRequest(outcome, null)),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        // TryAddWithoutValidation: HttpClient's typed If-Match header expects a quoted ETag;
        // the server just reads the raw header string, so skip that client-side parsing.
        request.Headers.TryAddWithoutValidation("If-Match", rowVersion.ToString());
        return client.SendAsync(request);
    }
}
