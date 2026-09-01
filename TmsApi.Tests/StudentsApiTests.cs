using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace TmsApi.Tests;

public class StudentsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StudentsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllStudents_ReturnsOkStatus()
    {
        var response = await _client.GetAsync("/api/students/all");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateStudent_ValidData_ReturnsCreated()
    {
        var regNum = "TEST-REG-" + System.Guid.NewGuid().ToString("N")[..6];
        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            registrationNumber = regNum,
            name = "Test Automated Student",
            age = 22,
            gpa = 3.75
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
