using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/certificates")]
[ApiVersion("2.0")]
[Tags("Certificates (v2)")]
public sealed class CertificatesController(
    ICertificateService certificates,
    TmsDbContext dbContext) : ControllerBase
{
    public sealed record IssueRequest(int StudentId, string CourseCode);

    [HttpGet]
    public async Task<IActionResult> GetAllCertificates([FromQuery] int? studentId, CancellationToken ct)
    {
        var query = dbContext.Certificates
            .Include(c => c.Student)
            .Include(c => c.Course)
            .AsNoTracking();

        if (studentId.HasValue)
        {
            query = query.Where(c => c.StudentId == studentId.Value);
        }

        var list = await query.Select(c => new
        {
            c.Id,
            c.SerialNumber,
            c.IssuedAt,
            c.StudentId,
            StudentName = c.Student.Name,
            c.CourseId,
            CourseCode = c.Course.Code,
            CourseTitle = c.Course.Title
        }).ToListAsync(ct);

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssueRequest req, CancellationToken ct)
    {
        try
        {
            var result = await certificates.IssueCertificateAsync(req.StudentId, req.CourseCode, ct);

            // Also record certificate in DB if not existing
            var course = await dbContext.Courses.FirstOrDefaultAsync(c => c.Code == req.CourseCode, ct);
            if (course != null)
            {
                var serial = $"CERT-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                var certificate = new Certificate
                {
                    SerialNumber = serial,
                    StudentId = req.StudentId,
                    CourseId = course.Id,
                    IssuedAt = DateTime.UtcNow
                };
                dbContext.Certificates.Add(certificate);
                await dbContext.SaveChangesAsync(ct);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Certificate request rejected",
                detail: ex.Message);
        }
    }
}