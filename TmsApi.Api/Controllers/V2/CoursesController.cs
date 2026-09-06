using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Application.Queries;
using TmsApi.Application.Services;
using TmsApi.Application.Utilities;

namespace TmsApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICourseService _courseService;

    public CoursesController(IMediator mediator, ICourseService courseService)
    {
        _mediator = mediator;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] PagedRequest? paging,
        CancellationToken ct)
    {
        var paged = paging ?? new PagedRequest();
        var courses = await _mediator.Send(new GetCoursesQuery(paged), ct);
        var shaped = courses.Items.ShapeData(fields, CourseDtoFields.Allowed);

        var links = new List<LinkDto>
        {
            new(Url.Action(nameof(GetCourses), new { page = courses.Page, fields })!, "self", "GET")
        };
        if (courses.HasNext)
        {
            links.Add(new(Url.Action(nameof(GetCourses), new { page = courses.Page + 1, fields })!, "next", "GET"));
        }
        if (courses.HasPrevious)
        {
            links.Add(new(Url.Action(nameof(GetCourses), new { page = courses.Page - 1, fields })!, "prev", "GET"));
        }

        return Ok(new
        {
            Data = shaped,
            Meta = new
            {
                courses.TotalCount,
                courses.Page,
                courses.TotalPages,
                courses.HasNext,
                courses.HasPrevious
            },
            Links = links
        });
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetCourse(string code, CancellationToken ct)
    {
        var course = await _mediator.Send(new GetCourseQuery(code), ct);
        if (course is null) 
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"Course with code '{code}' was not found.",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(new
        {
            Data = course,
            Links = new[]
            {
                new LinkDto(Url.Action(nameof(GetCourse), new { code })!, "self", "GET"),
                new LinkDto($"/api/v2/enrollments", "enroll", "POST")
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Course code and title are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (await _courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with the code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var created = await _courseService.CreateAsync(request, ct);
        return CreatedAtAction(
            nameof(GetCourse), 
            new { version = "2.0", code = created.Code }, 
            created
        );
    }
}