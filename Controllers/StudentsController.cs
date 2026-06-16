using Microsoft.AspNetCore.Mvc;
namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
    [HttpGet("all")]
    public IActionResult GetAllStudents()
    {
        var students = new[]
        {
        new {Id= "ST123" , Name= "Feleke", Email= "feleke@gmail.com"},
        new {Id= "ST124" , Name= "Abebe", Email= "abebe@gmail.com"} 
        };
        return Ok(students);
    }
    [HttpGet("{id}")]
    public IActionResult GetStudentById(string id)
    {
        if(id != "ST123")
        {
            return NotFound(new { message = $"Student with ID {id} not found."});
        }
        var student = new {Id = "ST123", Name = "Kibru", Email = "kibru@gmail.com"};
        return Ok(student);
    }
}
}