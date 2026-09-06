using MediatR;

using TmsApi.Application.Dtos;

namespace TmsApi.Application.Queries;

public record GetCoursesQuery(PagedRequest Paging): IRequest<PagedResponse<CourseResponseDto>>;

public record GetCourseQuery(string Code) : IRequest<CourseDto?>;