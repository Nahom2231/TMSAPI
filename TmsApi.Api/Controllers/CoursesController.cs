using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Application.Dtos;
using TmsApi.Application.Services;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly LinkGenerator _linkGenerator;
    private readonly ICachedCourseService _cachedCourseService;
    private readonly IAuthorizationService _authorizationService;

    public CoursesController(
        ICourseService courseService, 
        LinkGenerator linkGenerator, 
        ICachedCourseService cachedCourseService, 
        IAuthorizationService authorizationService)
    {
        _courseService = courseService;
        _linkGenerator = linkGenerator;
        _cachedCourseService = cachedCourseService;
        _authorizationService = authorizationService;
    }

    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription("Return course details with HATEOAS links. Return 404 if the course doesn't exist")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(id, ct);
        if (course is null)
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"Course with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });

        string selfPath = _linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })
            ?? $"/api/courses/{id}";

        string enrollmentsPath = _linkGenerator.GetPathByAction(
            HttpContext,
            action: "GetEnrollments",
            controller: "Enrollments",
            values: new { courseId = id })
            ?? $"/api/courses/{id}/enrollments";

        var links = new List<LinkDto>
        {
            new(selfPath, "self", "GET"),
            new(selfPath, "update", "PUT"),
            new(selfPath, "delete", "DELETE"),
            new(enrollmentsPath, "enrollments", "GET")
        };

        if (course.EnrollmentCount < course.MaxCapacity)
        {
            links.Add(new LinkDto(enrollmentsPath, "enroll", "POST"));
        }

        var detailDto = await _cachedCourseService.GetCourseAsync(course.Code, ct);
        var responseDto = (detailDto ?? new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links = links
        }) with { Links = links };

        return Ok(responseDto);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDetailDto request, CancellationToken ct)
    {
        var course = await _courseService.GetEntityByIdAsync(id, ct);
        if (course == null) return NotFound(new ProblemDetails
        {
            Title = "Course Not Found",
            Detail = $"Course with ID {id} was not found.",
            Status = StatusCodes.Status404NotFound
        });

        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin") && !string.IsNullOrEmpty(course.InstructorId))
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }
        }

        await _courseService.UpdateAsync(id, request, ct);
        await _cachedCourseService.InvalidateCourseCacheAsync(ct);

        return Ok(new { message = "Course updated successfully." });
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var deleted = await _courseService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"Course with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        await _cachedCourseService.InvalidateCourseCacheAsync(ct);
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Course Code and Title are required.",
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

        var result = await _courseService.CreateAsync(request, ct);
        await _cachedCourseService.InvalidateCourseCacheAsync(ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _courseService.GetCoursesAsync(request ?? new PagedRequest(), ct);
        return Ok(result);
    }
}
