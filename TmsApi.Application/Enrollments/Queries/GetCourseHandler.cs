using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TmsApi.Application.Dtos;
using TmsApi.Application.Services;

namespace TmsApi.Application.Queries;

public class GetCourseHandler : IRequestHandler<GetCourseQuery, CourseDto?>
{
    private readonly ICourseService _courseService;

    public GetCourseHandler(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<CourseDto?> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByCodeAsync(request.Code, cancellationToken);
        if (course is null)
        {
            return null;
        }

        return new CourseDto(
            course.Id,
            course.Code,
            course.Title,
            course.MaxCapacity,
            course.Enrollments?.Count ?? 0
        );
    }
}
