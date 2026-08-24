using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace TmsApi.Tests;
public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    [Fact]
    public async Task GetCourses_ReturnsOkAndPagedJson()
    {
        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");

        //Assert

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedCoursesJson>();
        Assert.NotNull(page?.Items);


    }
    [Fact]
    
public async Task CreateCourse_InvalidCode_ReturnsValidationError()
{
    var response = await _client.PostAsJsonAsync("/api/v2.0/courses", new
    {
        code = "",
        title = "Intro to TMS Security",
        maxCapacity = 30
    });

    var body = await response.Content.ReadAsStringAsync();
    
    // This will print the status and response body if it fails
    Assert.True(
        response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
        $"Expected 400/422 but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
}
    private sealed class PagedCoursesJson
    {
        public List<CourseRowJson> Items {get; set; } = new();
        public int TotalCount {get; set;}
    }
    private sealed class CourseRowJson
    {
        public int Id {get; set; }
        public string Code {get; set;}= "";
        public string Title { get; set;} ="";

        public int MaxCapacity {get; set; }

        public int EnrollmentCount { get; set;}
    }
}