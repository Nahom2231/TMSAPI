using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;
using TmsApi.Application;
using TmsApi.Application.Common;
namespace TmsApi.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ITmsDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ITmsDbContext context, ILogger<EnrollmentService> _logger)
    {
        _context = context;
        this._logger = _logger;
    }

    public async Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    
    public async Task<EnrollmentResponseDto?> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Student {StudentId} enrolled in course {CourseId}", request.StudentId, courseId);

        var result = await GetByIdAsync(courseId, enrollment.Id, ct);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to retrieve enrollment tracking item after generation.");
        }
        return result;
    }

    public async Task<IEnumerable<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,   // Fixed ordering to match GetByIdAsync context
                e.StudentId,  // Fixed ordering to match GetByIdAsync context
                e.EnrolledAt
            ))
            .ToListAsync(ct);
    }
}