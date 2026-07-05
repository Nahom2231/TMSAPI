using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;
using TmsApi.Controllers.Configurations.Dtos;
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

        public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)=>
        await context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

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
   public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        IQueryable<Course> query = context.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            
            query = query.Where(c => EF.Functions.ILike(c.Title, $"%{request.Search}%") 
            || EF.Functions.ILike(c.Code, $"%{request.Search}%"));

        }

       var totalCount = await query.CountAsync(ct);
       query = request.OrderBy.ToLower() switch
       {
           "code" => request.descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
           "maxcapacity" => request.descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
           _=> request.descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
       };
       var items =await query
       .Skip((request.Page - 1) * request.pageSize)
       .Take(request.pageSize)
       .Select(c => new CourseResponseDto(
           c.Id,
           c.Code,
           c.Title,
           c.MaxCapacity,
           c.Enrollments.Count
       ))
       .ToListAsync(ct);

       return new PagedResponse<CourseResponseDto>
       {
           Items = items,
           TotalCount = totalCount,
           Page = request.Page,
           PageSize = request.pageSize
       };

    }

}