using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaseAuth.Api.Contracts;
using CaseAuth.Api.Controllers;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Tests;

// Own class (and so its own ApiFactory/database): a reset deletes every FIRM-A case, which would
// pull the rug out from under tests sharing CaseAuthApiTests' database.
public class DemoResetTests(ApiFactory factory) : IClassFixture<ApiFactory>
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

    [Fact]
    public async Task Reset_AsAnalyst_IsForbidden()
    {
        var response = await ClientFor("analyst1").PostAsync("/api/demo/reset", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reset_ReplacesFirmCasesWithPersonas_ReadyForDecision_AndLeavesOtherFirmsAlone()
    {
        var analyst = ClientFor("analyst1");
        var supervisor = ClientFor("supervisor");
        var stale = await analyst.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Old Case", null, null, null));
        var staleCase = (await stale.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;
        var otherFirm = await ClientFor("analyst2").PostAsJsonAsync("/api/cases", new CreateCaseRequest("Firm B Case", null, null, null));
        Assert.Equal(HttpStatusCode.Created, otherFirm.StatusCode);

        var reset = await supervisor.PostAsync("/api/demo/reset", null);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(5, (await reset.Content.ReadFromJsonAsync<DemoResetResponse>(JsonOptions))!.CasesCreated);

        var cases = (await analyst.GetFromJsonAsync<List<CaseResponse>>("/api/cases", JsonOptions))!;
        Assert.Equal(5, cases.Count);
        Assert.DoesNotContain(cases, c => c.Id == staleCase.Id);
        Assert.All(cases, c => Assert.Equal(CaseStatus.AwaitingDecision, c.Status));

        // Risk comes back on the case itself: the shell company is High, the clean applicant Low.
        Assert.Equal(FindingSeverity.High, cases.Single(c => c.ApplicantFullName == "Bluewater Meridian Holdings LLC").RiskTier);
        Assert.Equal(FindingSeverity.Low, cases.Single(c => c.ApplicantFullName == "Maria Elena Torres").RiskTier);

        // Seeded findings cite real extracted fields, and the document images are readable.
        var shell = cases.Single(c => c.ApplicantFullName == "Bluewater Meridian Holdings LLC");
        var input = (await analyst.GetFromJsonAsync<AiReviewInputResponse>($"/api/cases/{shell.Id}/ai-review-input", JsonOptions))!;
        Assert.All(input.Findings, f => Assert.NotEmpty(f.SourceFieldIds));
        var docs = (await analyst.GetFromJsonAsync<List<DocumentResponse>>($"/api/cases/{shell.Id}/documents", JsonOptions))!;
        var image = await analyst.GetAsync($"/api/cases/{shell.Id}/documents/{docs[0].Id}/content");
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);

        // Another firm's cases are untouched.
        var firmBCases = (await ClientFor("analyst2").GetFromJsonAsync<List<CaseResponse>>("/api/cases", JsonOptions))!;
        Assert.Single(firmBCases);
    }

    [Fact]
    public async Task CaseRisk_CombinesFindingScores()
    {
        var analyst = ClientFor("analyst1");
        var created = await analyst.PostAsJsonAsync("/api/cases", new CreateCaseRequest("Risk Case", null, null, null));
        var c = (await created.Content.ReadFromJsonAsync<CaseResponse>(JsonOptions))!;
        Assert.Equal(0, c.RiskScore);
        Assert.Equal(FindingSeverity.Low, c.RiskTier);

        foreach (var score in new[] { 0.5, 0.5 })
        {
            await analyst.PostAsJsonAsync($"/api/cases/{c.Id}/findings",
                new CreateFindingRequest(FindingSeverity.Medium, FindingSource.Ai, "TEST", "test", score, null));
        }

        var updated = (await analyst.GetFromJsonAsync<CaseResponse>($"/api/cases/{c.Id}", JsonOptions))!;
        Assert.Equal(0.75, updated.RiskScore, precision: 6);
        Assert.Equal(FindingSeverity.Medium, updated.RiskTier);
    }
}
