using System.Data;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Application.Services;

namespace TmsApi.Controllers;

[ApiController]

[Route("api/courses/{courseId:int}/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof (ProblemDetails), StatusCodes.Status500InternalServerError)]


public class EnrollmentsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(
        ICourseService courseService, 
        IEnrollmentService enrollmentService)
    {
        _courseService = courseService;
        _enrollmentService = enrollmentService;
    }

    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrollments for a course")]
    public async Task<IActionResult> GetEnrollments(int courseId, CancellationToken ct)
    {
        // 1. Confirm the parent course exists
        var course = await _courseService.GetByIdAsync(courseId, ct);
        if (course is null)
        {
            return NotFound();
        }

        // 2. Return the list of enrollments for this course
        var enrollments = await _enrollmentService.GetByCourseAsync(courseId, ct);
        return Ok(enrollments);
    }
     [HttpGet("{id:int}" , Name = nameof(GetEnrollment))]
     [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
     [EndpointSummary("Get one enrollment for a course")]
     public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await _enrollmentService.GetByIdAsync(courseId, id, ct);
         return enrollment is not null ? Ok(enrollment) : NotFound();
    }
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentResponseDto),  StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
     [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Enroll a student in a course")]
    [EndpointDescription("Returns 404 if the course does not exist, 409 if the course has reached MaxCapacity")]
    public async Task<IActionResult> EnrollStudent(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var course= await _courseService.GetByIdAsync(courseId, ct);

        if (course is null)
        {
            return NotFound();
        }
        if(course.EnrollmentCount >=course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"The course {course.Title} has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var enrollment= await _enrollmentService.CreateAsync(courseId, request, ct);
         return CreatedAtAction(
            nameof(GetEnrollment),
           new { courseId = courseId, id = enrollment.Id },
           enrollment
         );
    }
    [HttpGet(Name = "GetEnrollments")]
    public IActionResult GetEnrollments (int courseId)
    {
        return Ok();
    }

   
}


