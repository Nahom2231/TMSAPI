using Microsoft.AspNetCore.Mvc;
namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
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
    }
}