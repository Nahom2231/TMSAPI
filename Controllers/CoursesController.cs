using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Entities;
using TmsApi.Services;
using Microsoft.AspNetCore.Routing;

using TmsApi.Controllers.Configurations.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
namespace TmsApi.Controllers

{
    [ApiController]
    [Route("api/courses")]
   public class CoursesController : ControllerBase
   {
      private readonly ICourseService _courseService;
      private readonly LinkGenerator _linkGenerator;

      public CoursesController(ICourseService courseService, LinkGenerator linkGenerator)
      {
         _courseService = courseService;
         _linkGenerator = linkGenerator;
      }

      [HttpGet("{id:int}", Name = nameof(GetCourseById))]
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

         var detailDto = new CourseDetailDto
         {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            
            Links = links
         };

         return Ok(detailDto);
      }

      [HttpPut("{id:int}")]
      public IActionResult UpdateCourse(int id) => Ok();

      [HttpDelete("{id:int}")]
      public IActionResult DeleteCourse(int id) => NoContent();

      [HttpPost]
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
         return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
      }

      [HttpGet]
      public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
      {
         var result = await _courseService.GetCoursesAsync(request, ct);
         return Ok(result);
      }
   }
}

