using System.Runtime.Versioning;
using  Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TmsApi.Api.Controllers;

[ApiController]

[Route("api/transcripts")]

public class TranscriptsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]

    public async Task <IActionResult> RequestTranscript([FromBody]object? _)
    {
        await Task.Delay(200);
        return Ok();
    }

}