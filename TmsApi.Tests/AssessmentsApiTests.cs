using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace TmsApi.Tests;

public class AssessmentsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AssessmentsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetResults_WhenAnonymous_Returns401Unauthorized()
    {
        // Act - Call sensitive results route without token
        var response = await _client.GetAsync("/api/assessments/results");

        // Assert - Anonymous calls must be rejected with 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAssessments_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/assessments");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
