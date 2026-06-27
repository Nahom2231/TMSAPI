using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Dtos;

namespace TmsApi.Services;

public class CourseService : ICourseService
{
    private readonly TmsDbContext context;
    private readonly ILogger<CourseService> logger;

    public CourseService(TmsDbContext context, ILogger<CourseService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)=>
          
        await context.Courses
        .AsNoTracking()
        .Where(c => c.Id == id )
        .Select(c => new CourseResponseDto(
            c.Id, 
            c.Code, 
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count
            ))
        .FirstOrDefaultAsync(ct);
    

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course {CourseId}  ({Code})", course.Id, course.Code);
       return new CourseResponseDto(
        course.Id,
        course.Code,
        course.Title,
        course.MaxCapacity,
        0 
    );
}
}