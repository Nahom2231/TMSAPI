using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Common;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Enrollments.Commands;

public class EnrollmentStudentHandler(TmsDbContext context)
:IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
    EnrollStudentCommand command, CancellationToken ct)
    {
  var course = await context.Courses
  .Include(c => c.Enrollments)
  .FirstOrDefaultAsync(c =>c.Code == command.CourseCode, ct);
   
    if (course is null)
     return Result<EnrollmentCreated, EnrollmentError>.Failure(
  EnrollmentError.CourseNotFound(command.CourseCode));

  if (course.Enrollments.Count >= course.MaxCapacity)
  return Result<EnrollmentCreated, EnrollmentError>.Failure(
  EnrollmentError.CourseFull(course.Title, course.MaxCapacity));

 var alreadyEnrolled = await context.Enrollments
 .AnyAsync(e =>e.StudentId == command.StudentId && e.CourseId == course.Id, ct);
 if(alreadyEnrolled)
 return Result<EnrollmentCreated, EnrollmentError>.Failure(
    EnrollmentError.AlreadyEnrolled(command.StudentId, command.CourseCode));
    var enrollment = new Enrollment
    {
      StudentId = command.StudentId,
      CourseId = course.Id,
      EnrolledAt = DateTime.UtcNow  
    };

    context.Enrollments.Add(enrollment);
    await context.SaveChangesAsync(ct);

    return Result<EnrollmentCreated, EnrollmentError>.Success(
        new EnrollmentCreated(enrollment.Id, enrollment.StudentId, course.Code));
    }
}