using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Data;
namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly TmsDbContext _context;

        public StudentsController(TmsDbContext context)
        {
            _context = context;
        }

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

        [HttpGet]
        public async Task<IActionResult> GetPagedStudents(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var students = await _context.Students
                .OrderBy(s => s.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(students);
        }

        [HttpGet("top-courses")]
        public async Task<IActionResult> GetTopCourses(CancellationToken cancellationToken = default)
        {
            var topCourses = await _context.Enrollments
                .GroupBy(e => e.Course.Title)
                .Select(g => new { CourseTitle = g.Key, EnrollmentCount = g.Count() })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(5)
                .ToListAsync(cancellationToken);

            return Ok(topCourses);
        }
       [HttpGet("Exercise7a")]
       public async Task<IActionResult> GetExercise7A(CancellationToken cancellationToken)
        {
            var students= await _context.Students.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var s in students)
            {
               var count = await _context.Enrollments
               .AsNoTracking()
               .CountAsync(e => e.StudentId == s.Id, cancellationToken); 
               Console.WriteLine($"{s.Name}: {count} enrollments");
            }
            return Ok(students);
        }

        [HttpGet("Exercise7b")]

        public async Task<IActionResult> GetExercise7B(CancellationToken cancellationToken)
        {
            var report= await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                StudentName = s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .OrderByDescending(s => s.EnrollmentCount)
            .ToListAsync(cancellationToken);

            foreach (var r in report)
            {
                Console.WriteLine($"{r.StudentName}: {r.EnrollmentCount} enrollments");
            }
            return Ok(report);
        }
        
    }
}
