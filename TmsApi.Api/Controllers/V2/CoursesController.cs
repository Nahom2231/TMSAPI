using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using MediatR;
using TmsApi.Application.Queries;
using TmsApi.Application.Dtos;
using TmsApi.Application.Utilities;
namespace TmsApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]

public class CoursesController : ControllerBase
{
    private readonly IMediator mediator;

    public CoursesController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]

    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] PagedRequest paging,
        CancellationToken ct)
    {
       var courses = await mediator.Send(new GetCoursesQuery(paging), ct);
        var shaped = courses.Items.ShapeData(fields, CourseDtoFields.Allowed);

        var links = new List<LinkDto>
        {
            new(Url.Action(nameof(GetCourses), new {page = courses.Page, fields})!, "self", "Get")

        };
        if (courses.HasNext)
        {
            links.Add(new(Url.Action(nameof(GetCourses), new  {page = courses.Page + 1,  fields})!, "next", "Get"));
        }
        if (courses.HasPrevious)
        {
            links.Add(new (Url.Action(nameof(GetCourses), new { page = courses.Page -1, fields})!, "prev", "Get"));
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
        var course = await mediator.Send(new GetCourseQuery(code), ct);
        if (course is null) return NotFound();

         return Ok(new
         {
           Data = course,
           Links = new[]
           {
             new LinkDto(Url.Action(nameof(GetCourse), new{ code})!, "self", "GET"),
             new LinkDto(Url.Action("Enroll", "Enrollments", new {courseCode = code})!, "enroll", "POST")
         }
        });
    }
    
}