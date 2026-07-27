using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TmsApi.Application.Dtos;
using TmsApi.Application.Services;

namespace TmsApi.Application.Queries;

public class GetCoursesHandler : IRequestHandler<GetCoursesQuery, PagedResponse<CourseResponseDto>>
{
    private readonly ICourseService _courseService;

    public GetCoursesHandler(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<PagedResponse<CourseResponseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetCoursesAsync(request.Paging, cancellationToken);
    }
}