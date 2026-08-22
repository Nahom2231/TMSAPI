using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;
using TmsApi.Application.Services;
using Microsoft.AspNetCore.Routing;
using TmsApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
namespace TmsApi.Controllers;


    [Authorize(Roles ="Instructor , Admin" )]
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
   
      public CoursesController(ICourseService courseService, LinkGenerator linkGenerator , ICachedCourseService cachedCourseService)
      {
         _courseService = courseService;
         _linkGenerator = linkGenerator;
         _cachedCourseService = cachedCourseService;
         _authorizationService= authorizationService;
      }

      [HttpGet("{id:int}", Name = nameof(GetCourseById))]
      [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
      [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status404NotFound)]
      [EndpointSummary("Get a course by ID")]
      [EndpointDescription("Return course details with HATEOAS links. Return 404 if the course doesn't exists")]

      
      public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
      {
         var course = await _courseService.GetByIdAsync(id, ct);
         if (course is null)
            return NotFound();

         string selfPath = _linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })
            ?? throw new InvalidOperationException($"Route {nameof(GetCourseById)} could not be generated.");

         string enrollmentsPath = _linkGenerator.GetPathByAction(
            HttpContext,
            action: "GetEnrollments",
            controller: "Enrollments",
            values: new { courseId = id })
            ?? throw new InvalidOperationException("Enrollments route path could not be generated.");

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

       var responseDto = detailDto with { Links = links };
        return Ok(responseDto);
      }

      [HttpPut("{id:int}")]
      public async Task<IActionResult> UpdateCourse(int id, [FromBody] CourseDetailDto request, CancellationToken ct)
      {

         var course = await _courseService.GetByIdAsync(id, ct);
         if(course == null) return NotFound();

         var authResult = await _authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
         if (!authResult.Succeeded)
      {
         return Forbid();
      }
         await _cachedCourseService.InvalidateCourseCacheAsync(ct);

         return Ok();
      }

      [HttpDelete("{id:int}")]
      public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
      {
         await _cachedCourseService.InvalidateCourseCacheAsync(ct);

         return NoContent();
      }

      [HttpPost]
      [ProducesResponseType(typeof(CourseResponseDto),StatusCodes.Status201Created)]
      [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
      [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
      [EndpointSummary("Create a new course")]
      [EndpointDescription("Creates a course with a unique code.Return409 if the course code already exists")]
      public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
      {
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
      public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
      {
         var result = await _cachedCourseService.GetAllCoursesAsync(ct);
         return Ok(result);
      }
   }


