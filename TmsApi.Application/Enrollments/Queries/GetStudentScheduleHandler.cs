using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Common; // Adjust if ITmsDbContext is in a different folder
using TmsApi.Enrollments;
namespace TmsApi.Enrollments.Queries;
using TmsApi.Application.Common;

public class GetStudentScheduleHandler(ITmsDbContext context)
: IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(
        GetStudentScheduleQuery query, CancellationToken ct)
    {
        var enrollments = await context.Enrollments
        .AsNoTracking()
        .Include(e=>e.Course)
        .Where(e=>e.StudentId==query.StudentId)
        .ToListAsync(ct);
        var items= enrollments.Select(e=>new ScheduleItemDto(
            e.Course.Code,
            e.Course.Title,
            "TBD")).ToList();

            return new ScheduleDto(query.StudentId, items);

    }
}

public sealed record GetStudentScheduleQuery(int StudentId) : IRequest<ScheduleDto>;
