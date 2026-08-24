using MediatR;
using TmsApi.Application.Services;
using TmsApi.Domain.Common;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentHandler : IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICourseService _courseService;

    public EnrollStudentHandler(IEnrollmentService enrollmentService, ICourseService courseService)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
    }

    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
        EnrollStudentCommand command, CancellationToken ct)
    {
        var course = await _courseService.GetByCodeAsync(command.CourseCode, ct);
        if (course is null)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseNotFound(command.CourseCode));
        }

        var alreadyEnrolled = await _enrollmentService.ExistsAsync(command.StudentId, command.CourseCode, ct);
        if (alreadyEnrolled)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.AlreadyEnrolled(command.StudentId, command.CourseCode));
        }

        if (course.Enrollments.Count >= course.MaxCapacity)
        {
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseFull(course.Title, course.MaxCapacity));
        }

        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id
        };

        await _enrollmentService.AddAsync(enrollment, ct);

        return Result<EnrollmentCreated, EnrollmentError>.Success(
            new EnrollmentCreated(enrollment.Id, command.StudentId, command.CourseCode));
    }
}