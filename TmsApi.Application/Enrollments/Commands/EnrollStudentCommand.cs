using MediatR;
using TmsApi.Domain.Entities;
using TmsApi.Domain.Common; 

namespace TmsApi.Application.Enrollments.Commands;

public record EnrollStudentCommand(int StudentId, string CourseCode) 
    : IRequest<Result<EnrollmentCreated, EnrollmentError>>;

public record EnrollmentCreated(int EnrollmentId, int StudentId, string CourseCode);