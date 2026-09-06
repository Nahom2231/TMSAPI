using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Common;

public interface ITmsDbContext
{
    DbSet<Student> Students { get; }
    DbSet<Course> Courses { get; }
    DbSet<Enrollment> Enrollments { get; }
    
    DbSet<Assessment> Assessments { get; }
    DbSet<Certificate> Certificates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}