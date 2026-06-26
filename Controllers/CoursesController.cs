using Microsoft.AspNetCore.Mvc;

using TmsApi.Entities;
using TmsApi.Services;

namespace TmsApi.Controllers

{
    [ApiController]
    [Route("api/courses")]
    public class CourseController(ICourseService courseService) : ControllerBase
    {
        [HttpGet("all")]
        public IActionResult GetAllCourses()
        {
            var courses = new[]
            {
            new { Code ="CS101" , Title = "Introduction to Computer Science" , Credits=4},
            new { Code ="CS201" , Title = "Data Structures and Algorithms" , Credits=4}
            };
            return Ok(courses);
        }
        [HttpGet("{id}")]
        public IActionResult GetActionResult(string id)
        {
            if (id!= "CS101")
            {
                return NotFound(new { message = $"Course with Code {id} not found"});
            }
            var course = new { Code = id, Title = "Introduction to Computer Science", Credits = 4 };
            return Ok(course);
        }
        
        
        
         [HttpGet("{id:int}" , Name = nameof(GetCourseById))]   
         public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
         {
            var course = await courseService.GetByIdAsync(id , ct);
            if (course == null)
            {
                return NotFound();
            }
            return Ok(course);
         }

         [HttpPost]
         public async Task<IActionResult> CreateCourse(Course course, CancellationToken ct)
         {
            await courseService.CreateAsync(course, ct);

            return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
         }
        }
    }
