using CaseAuth.Api.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace CaseAuth.Api.Tests;

public class AiReviewerRegistrationTests
{
    [Fact]
    public void RemoteMode_ResolvesRemoteReviewer()
    {
        using var factory = new ApiFactory { AiReviewMode = "Remote" };
        using var scope = factory.Services.CreateScope();

        Assert.IsType<RemoteAiReviewer>(scope.ServiceProvider.GetRequiredService<IAiReviewer>());
    }
}
