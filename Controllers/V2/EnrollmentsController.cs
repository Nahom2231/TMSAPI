using MediatR;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Enrollments.Commands;
using TmsApi.Enrollments.Queries;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v2/enrollments")]
public class EnrollmentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollStudentCommand command,
        CancellationToken ct)
    {
        var result =await mediator.Send(command, ct);
        return  result.Match<IActionResult>(
            successValue => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = successValue.StudentId }, successValue),
                error => BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title= "Enrollment Failed",
                    Detail = error.Message,
                    Extensions = {["errorCode"] = error.Code}
                })
            );
        
    }
    [HttpGet("{studentId:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var query =new GetStudentScheduleQuery(studentId);
        var schedule = await mediator.Send(query, ct);
        return Ok(schedule);
    }
    
}