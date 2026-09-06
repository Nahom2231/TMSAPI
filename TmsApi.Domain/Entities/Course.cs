namespace TmsApi.Domain.Entities;



public enum CourseStatus
{
    Active,
    Suspended,

    Archived
}
public class Course
{
    public int Id { get; set; }


    public required string Code {get;  set;}
    public required string Title { get; set; }

    public string? InstructorId { get; set; }

    public int  MaxCapacity { get; set; }

    public CourseStatus Status
    {
        get => field;
        set
        {
            field = value;
            if (value == CourseStatus.Archived)
            {
                MaxCapacity = 0;
            }
        }
    }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Assessment> Assessments { get; set; } =new List<Assessment>();
}