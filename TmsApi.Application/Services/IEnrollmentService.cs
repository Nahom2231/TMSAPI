using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);
    Task<EnrollmentResponseDto?> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
    Task<IEnumerable<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);

    // Add these two methods:
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct = default);
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);
}
