using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Grading;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/assessments")]
[Tags("Assessments & Grading")]
[Produces("application/json")]
public class AssessmentsController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly GradingService _gradingService;

    public AssessmentsController(TmsDbContext context)
    {
        _context = context;
        _gradingService = new GradingService();
    }

    public record CreateAssessmentRequest(int CourseId, string Title, decimal MaxScore, decimal Weight);
    public record SubmitGradeRequest(int StudentId, int CourseId, decimal Score);

    [Authorize]
    [HttpGet("results")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Get all assessment results")]
    [EndpointDescription("Secured endpoint returning student performance and calculated letter grades. Rejects anonymous callers with 401.")]
    public async Task<IActionResult> GetAssessmentResults(CancellationToken ct)
    {
        var enrollments = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking()
            .ToListAsync(ct);

        var results = enrollments.Select(e =>
        {
            var letterGrade = _gradingService.CalculateFromEnrollmentGrade(e.Grade);
            return new
            {
                EnrollmentId = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student?.Name ?? "Unknown",
                CourseId = e.CourseId,
                CourseCode = e.Course?.Code ?? "N/A",
                CourseTitle = e.Course?.Title ?? "N/A",
                RawGrade = e.Grade,
                LetterGrade = letterGrade.ToString(),
                IsPassing = letterGrade is GradeLevel.Pass or GradeLevel.Distinction,
                EnrolledAt = e.EnrolledAt
            };
        });

        return Ok(results);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAssessments([FromQuery] int? courseId, CancellationToken ct)
    {
        var query = _context.Assessments.Include(a => a.Course).AsNoTracking();
        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }

        var list = await query.Select(a => new
        {
            a.Id,
            a.Title,
            a.MaxScore,
            a.Weight,
            a.CourseId,
            CourseCode = a.Course.Code,
            CourseTitle = a.Course.Title
        }).ToListAsync(ct);

        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequest request, CancellationToken ct)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"Course with ID {request.CourseId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var assessment = new Assessment
        {
            CourseId = request.CourseId,
            Title = request.Title,
            MaxScore = request.MaxScore > 0 ? request.MaxScore : 100m,
            Weight = request.Weight > 0 ? request.Weight : 1.0m
        };

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAllAssessments), new { id = assessment.Id }, assessment);
    }

    [HttpPost("submit-grade")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> SubmitGrade([FromBody] SubmitGradeRequest request, CancellationToken ct)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId, ct);

        if (enrollment == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment Not Found",
                Detail = $"No enrollment found for Student {request.StudentId} in Course {request.CourseId}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        enrollment.Grade = Math.Clamp(request.Score, 0m, 100m);
        var letterGrade = _gradingService.CalculateFromEnrollmentGrade(enrollment.Grade);

        // Recalculate Student GPA from all graded enrollments
        var allStudentEnrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId && e.Grade.HasValue)
            .ToListAsync(ct);

        if (allStudentEnrollments.Any())
        {
            var averageGrade = allStudentEnrollments.Average(e => e.Grade!.Value);
            // 100-scale to 4.0 GPA scale conversion
            var student = await _context.Students.FindAsync(new object[] { request.StudentId }, ct);
            if (student != null)
            {
                student.GPA = Math.Round((averageGrade / 100m) * 4.0m, 2);
                student.AddGrade(new GradeRecord(enrollment.Course.Code, enrollment.Grade.Value, DateTime.UtcNow));
            }
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            Message = "Grade recorded successfully",
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            Score = enrollment.Grade,
            LetterGrade = letterGrade.ToString()
        });
    }
}
