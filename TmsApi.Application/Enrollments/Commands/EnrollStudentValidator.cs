using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("StudentId must be a positive number");

        RuleFor(x => x.CourseCode)
            .NotEmpty()
            .WithMessage("Course code is required");
    }
}