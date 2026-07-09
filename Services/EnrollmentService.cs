using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
using TmsApi.Services;
using TmsApi.Data;
using TmsApi.Dtos;
public class EnrollmentService : IEnrollmentService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        _context.Enrollments
            .AsNoTracking()
    .Where(e=>e.Id ==id && e.CourseId == courseId)
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

        if (result ==null)
        {
            throw new InvalidOperationException("Failed to retrieve enrollment tracking item after generation.");
        }
        return result;
    }
    public async Task<IEnumerable<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
{
    return await _context.Enrollments
        .Where(e => e.CourseId == courseId)
        .Select(e => new EnrollmentResponseDto(
        
             e.Id,
             e.StudentId,
             e.CourseId,
             e.EnrolledAt
            // Map other fields required by your DTO here...
        ))
        .ToListAsync(ct);
}
}