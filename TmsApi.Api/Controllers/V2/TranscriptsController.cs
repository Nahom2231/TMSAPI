using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v2/transcripts")]
[ApiVersion("2.0")]
[Tags("Transcripts (v2)")]
public class TranscriptController(
    Channel<TranscriptRequest> channel,
    ITranscriptStatusStore statusStore,
    TmsDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        [FromBody] TranscriptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, 
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await statusStore.GetReportIdForIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                var existingStatus = await statusStore.GetAsync(existing, ct);
                return Accepted(
                    Url.Action(nameof(GetStatus), new { id = existing }),
                    existingStatus);
            }
        }

        var reportId = Guid.NewGuid().ToString("N")[..12];
        var status = await statusStore.CreateAsync(reportId, request.StudentId, ct);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await statusStore.LinkIdempotencyKeyAsync(idempotencyKey, reportId, ct);

        var queuedRequest = request with { ReportId = reportId };
        await channel.Writer.WriteAsync(queuedRequest, ct);
        
        Response.Headers.RetryAfter = "3";
        return Accepted(
            Url.Action(nameof(GetStatus), new { id = reportId }), 
            status);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken ct)
    {
        var status = await statusStore.GetAsync(id, ct);
        return status is null
            ? NotFound(new ProblemDetails
            {
                Title = "Transcript not found",
                Detail = $"No transcript request with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            })
            : Ok(status);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadTranscript(string id, CancellationToken ct)
    {
        var status = await statusStore.GetAsync(id, ct);
        if (status is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transcript not found",
                Detail = $"Transcript '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var student = await dbContext.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.Id == status.StudentId, ct);

        var sb = new StringBuilder();
        sb.AppendLine("=================================================");
        sb.AppendLine("        TRAINING MANAGEMENT SYSTEM (TMS)        ");
        sb.AppendLine("              OFFICIAL TRANSCRIPT               ");
        sb.AppendLine("=================================================");
        sb.AppendLine($"Report ID:          {id}");
        sb.AppendLine($"Generated At:       {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Student ID:         {status.StudentId}");
        sb.AppendLine($"Student Name:       {student?.Name ?? "N/A"}");
        sb.AppendLine($"Registration No:    {student?.RegistrationNumber ?? "N/A"}");
        sb.AppendLine($"Cumulative GPA:     {student?.GPA:F2}");
        sb.AppendLine("-------------------------------------------------");
        sb.AppendLine("ENROLLED COURSES & GRADES:");
        sb.AppendLine("-------------------------------------------------");

        if (student?.Enrollments != null && student.Enrollments.Any())
        {
            foreach (var e in student.Enrollments)
            {
                sb.AppendLine($"* {e.Course?.Code,-10} | {e.Course?.Title,-30} | Score: {e.Grade?.ToString("F1") ?? "In Progress",-12}");
            }
        }
        else
        {
            sb.AppendLine("No enrollments recorded for this student.");
        }

        sb.AppendLine("=================================================");
        sb.AppendLine("Status: OFFICIAL - VERIFIED BY SYSTEM REGISTRAR");
        sb.AppendLine("=================================================");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/plain", $"TMS-Transcript-{status.StudentId}-{id}.txt");
    }
}