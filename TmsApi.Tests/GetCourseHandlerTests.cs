using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using TmsApi.Application.Dtos;
using TmsApi.Application.Queries;
using TmsApi.Application.Services;
using TmsApi.Domain.Entities;
using Xunit;

namespace TmsApi.Tests;

public class GetCourseHandlerTests
{
    [Fact]
    public async Task Handle_WhenCourseExists_ReturnsCourseDto()
    {
        // Arrange
        var courseService = Substitute.For<ICourseService>();
        var course = new Course
        {
            Id = 10,
            Code = "CS-201",
            Title = "Data Structures",
            MaxCapacity = 25,
            Enrollments = new List<Enrollment>
            {
                new() { Id = 1, CourseId = 10, StudentId = 1 }
            }
        };

        courseService.GetByCodeAsync("CS-201", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        var handler = new GetCourseHandler(courseService);
        var query = new GetCourseQuery("CS-201");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("CS-201", result.Code);
        Assert.Equal("Data Structures", result.Title);
        Assert.Equal(25, result.MaxCapacity);
        Assert.Equal(1, result.EnrollmentCount);
    }

    [Fact]
    public async Task Handle_WhenCourseDoesNotExist_ReturnsNull()
    {
        // Arrange
        var courseService = Substitute.For<ICourseService>();
        courseService.GetByCodeAsync("UNKNOWN", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(null));

        var handler = new GetCourseHandler(courseService);
        var query = new GetCourseQuery("UNKNOWN");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
