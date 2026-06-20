using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Linq.Expressions;
using TmsApi.Data;

namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly TmsDbContext _context;

        public TestController(TmsDbContext context)
        {
            _context = context;
        }

        [HttpGet("deferred")]
        public IActionResult TestDeferred()
        {
            Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
            var query = _context.Students.Where(s => s.GPA >= 3.0m);

            Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
            var orderedQuery = query.OrderBy(s => s.Name);

            Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
            var results = orderedQuery.ToList();

            Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
            return Ok(results);
        }

        private static bool IsHonorRoll(decimal gpa)
        {
            return gpa >= 3.5m;
        }

        [HttpGet("translation-fail")]
        public IActionResult TestTranslationFail()
        {
            try
            {
                Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
                var results = _context.Students
                    .Where(s => IsHonorRoll(s.GPA))
                    .ToList();
                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
 