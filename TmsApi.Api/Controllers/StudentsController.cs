using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
[Tags("Students")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public StudentsController(TmsDbContext context)
    {
        _context = context;
    }

    public record CreateStudentRequest(string RegistrationNumber, string Name, int Age, decimal? GPA);
    public record UpdateStudentRequest(string Name, int Age, decimal? GPA, bool IsActive);

    [HttpGet("all")]
    public async Task<IActionResult> GetAllStudents(CancellationToken ct)
    {
        var students = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.RegistrationNumber,
                s.Name,
                s.Age,
                s.GPA,
                s.IsActive,
                EnrollmentCount = s.Enrollments.Count,
                CertificateCount = s.Certificates.Count
            })
            .ToListAsync(ct);

        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudentById(string id, CancellationToken ct)
    {
        Student? student = null;
        if (int.TryParse(id, out var intId))
        {
            student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Certificates)
                .FirstOrDefaultAsync(s => s.Id == intId, ct);
        }
        else
        {
            student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Certificates)
                .FirstOrDefaultAsync(s => s.RegistrationNumber == id, ct);
        }

        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student Not Found",
                Detail = $"Student '{id}' does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new
        {
            student.Id,
            student.RegistrationNumber,
            student.Name,
            student.Age,
            student.GPA,
            student.IsActive,
            Enrollments = student.Enrollments.Select(e => new
            {
                e.Id,
                e.CourseId,
                CourseCode = e.Course?.Code,
                CourseTitle = e.Course?.Title,
                e.Grade,
                e.EnrolledAt
            }),
            Certificates = student.Certificates.Select(c => new
            {
                c.Id,
                c.SerialNumber,
                c.CourseId,
                c.IssuedAt
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var sLower = search.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(sLower) 
                || s.RegistrationNumber.ToLower().Contains(sLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var students = await query
            .OrderBy(s => s.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.RegistrationNumber,
                s.Name,
                s.Age,
                s.GPA,
                s.IsActive,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Items = students,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.RegistrationNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Registration Number and Name are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var exists = await _context.Students.AnyAsync(s => s.RegistrationNumber == request.RegistrationNumber, ct);
        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Student Already Exists",
                Detail = $"Student with Registration Number '{request.RegistrationNumber}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var student = new Student
        {
            RegistrationNumber = request.RegistrationNumber,
            Name = request.Name,
            Age = request.Age > 0 ? request.Age : 18,
            GPA = request.GPA ?? 0.0m,
            IsActive = true
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentRequest request, CancellationToken ct)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student Not Found",
                Detail = $"Student with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        student.Name = request.Name ?? student.Name;
        student.Age = request.Age > 0 ? request.Age : student.Age;
        if (request.GPA.HasValue) student.GPA = request.GPA.Value;
        student.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);
        return Ok(new { message = "Student updated successfully.", student });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken ct)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student Not Found",
                Detail = $"Student with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        student.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return NoContent();
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
        var students = await _context.Students
            .Include(s => s.Enrollments)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(students.Select(s => new
        {
            StudentName = s.Name,
            EnrollmentCount = s.Enrollments.Count
        }));
    }

    [HttpGet("Exercise7b")]
    public async Task<IActionResult> GetExercise7B(CancellationToken cancellationToken)
    {
        var report = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                StudentName = s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .OrderByDescending(s => s.EnrollmentCount)
            .ToListAsync(cancellationToken);

        return Ok(report);
    }

    [HttpPost("archive-old-enrollments")]
    public async Task<IActionResult> ArchiveEnrollments(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-2);

        int rowsAffected = await _context.Enrollments
            .Where(e => e.EnrolledAt < cutoffDate && !e.IsArchived)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsArchived, true),
                cancellationToken);

        return Ok(new { Message = "Archiving complete", RecordsUpdated = rowsAffected });
    }
}
