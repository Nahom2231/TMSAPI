using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;
using TmsApi.Application.Common;
namespace TmsApi.Application.Services;

public class CourseService : ICourseService
{
    private readonly ITmsDbContext context;
    private readonly ILogger<CourseService> logger;

    // Fixed constructor parameter to use ITmsDbContext instead of TmsDbContext
    public CourseService(ITmsDbContext context, ILogger<CourseService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id, 
                c.Code, 
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
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
        var searchLower = request.Search.ToLower();
        query = query.Where(c => c.Title.ToLower().Contains(searchLower) 
            || c.Code.ToLower().Contains(searchLower));
    }

    var totalCount = await query.CountAsync(ct);
    
    query = request.OrderBy.ToLower() switch
    {
        "code" => request.descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
        "maxcapacity" => request.descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
        _ => request.descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
    };

    var items = await query
        .Skip((request.Page - 1) * request.pageSize)
        .Take(request.pageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count // <-- Double check CourseResponseDto record matches this parameter count!
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
public async Task<Course?> GetByCodeAsync(string code, CancellationToken ct = default)
{
    return await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Code == code, ct);
}

}

