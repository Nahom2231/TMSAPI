using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Hubs;
using TmsApi.Enrollments.Queries;

namespace TmsApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/enrollments")]
[Tags("Enrollments (v2)")]
public class EnrollmentsController(
    IMediator mediator, 
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollStudentCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.Match<IActionResult>(
            successValue =>
            {
                // Real-time broadcast of new enrollment
                hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(
                    successValue.EnrollmentId.ToString(), 
                    $"Enrolled in {successValue.CourseCode}");

                return CreatedAtAction(
                    nameof(GetSchedule),
                    new { studentId = successValue.StudentId }, 
                    successValue);
            },
            error => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Enrollment Failed",
                Detail = error.Message,
                Extensions = { ["errorCode"] = error.Code }
            })
        );
    }

    [HttpGet("{studentId:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var query = new GetStudentScheduleQuery(studentId);
        var schedule = await mediator.Send(query, ct);
        return Ok(schedule);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id, "Approved");
        return Ok(new { message = $"Enrollment {id} approved successfully." });
    }
}
