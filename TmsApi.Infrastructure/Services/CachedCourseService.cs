using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Services; 
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public interface ICachedCourseService
{
    Task<CourseDetailDto?> GetCourseAsync(string code, CancellationToken ct);
    Task<IEnumerable<CourseDetailDto>> GetAllCoursesAsync(CancellationToken ct);
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}

public class CachedCourseService : ICachedCourseService
{
    private readonly HybridCache _cache;
    private readonly ICourseService _courseService;
    private readonly ILogger<CachedCourseService> _logger;

    private static class CacheKeys
    {
        public static string Course(string code) => $"Course:{code}";
        public const string CoursesTag = "Courses";
        public const string CoursesAll = "Courses:All";
    }
    
    public CachedCourseService(HybridCache cache, ICourseService courseService, ILogger<CachedCourseService> logger)
    {
        _cache = cache;
        _courseService = courseService;
        _logger = logger;
    }

    public async Task<CourseDetailDto?> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;
        var dto = await _cache.GetOrCreateAsync<CourseDetailDto?>(
            key,
            async token =>
            {
                dbHit = true;
                _logger.LogInformation("Cache MISS for {Key} - fetching from DB", key);

                var course = await _courseService.GetByCodeAsync(code, token);
                if (course is null) return null;

                return new CourseDetailDto
                {
                    Id = course.Id,
                    Code = course.Code,
                    Title = course.Title,
                    MaxCapacity = course.MaxCapacity,
                    EnrollmentCount = course.Enrollments?.Count ?? 0,
                    Links = Array.Empty<LinkDto>()
                };
            },
            tags: new[] { CacheKeys.CoursesTag },
            cancellationToken: ct);

        if (!dbHit)
            _logger.LogInformation("Cache HIT for {Key}", key);

        if (dbHit)
            TmsMeters.CacheMisses.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));
        else
            TmsMeters.CacheHits.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));

        return dto;
    }

    public async Task<IEnumerable<CourseDetailDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;
        var list = await _cache.GetOrCreateAsync<IEnumerable<CourseDetailDto>>(
            key,
            async token =>
            {
                dbHit = true;
                _logger.LogInformation("Cache MISS for {Key} - fetching from DB", key);
                var pagedResponse = await _courseService.GetCoursesAsync(new PagedRequest(), token);
                return pagedResponse.Items.Select(item => new CourseDetailDto
                {
                    Id = item.Id,
                    Code = item.Code,
                    Title = item.Title,
                    MaxCapacity = item.MaxCapacity,
                    EnrollmentCount = item.EnrollmentCount,
                    Links = Array.Empty<LinkDto>()
                }).ToList();
            },
            tags: new[] { CacheKeys.CoursesTag },
            cancellationToken: ct);

        if (!dbHit)
            _logger.LogInformation("Cache HIT for {Key}", key);

        if (dbHit)
            TmsMeters.CacheMisses.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));
        else
            TmsMeters.CacheHits.Add(1, new KeyValuePair<string, object?>("key.kind", "course"));

        return list;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        _logger.LogInformation("Invalidate course tag {Tag}", CacheKeys.CoursesTag);
        await _cache.RemoveByTagAsync(new[] { CacheKeys.CoursesTag }, ct);
    }
}