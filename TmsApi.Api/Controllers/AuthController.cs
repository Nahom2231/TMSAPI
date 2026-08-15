using System.Net;
using System.Net.Cache;
using System.Reflection.Metadata;
using System.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers;

[ApiController]

[Route("api/v1/auth")]

public class AuthController: ControllerBase
{
    [HttpPost("login")]
      public IActionResult Login(
        [FromBody] LoginRequest request,
        [FromServices]  IWebHostEnvironment env)
    {
        if(request.Username == "admin"  && request.Password == "Password123!")
        {
            var dummyJwt = "header.payload.signature-demo-token";

            Response.Cookies.Append("tms_auth", dummyJwt, new CookieOptions
            {
                HttpOnly=true,
                Secure =!env.IsDevelopment(),
                SameSite= SameSiteMode.Strict,
                Expires= DateTimeOffset.UtcNow.AddHours(2) 
            });
            return Ok(new UserProfileDto("System Admin", "Admin"));
        }
        return Unauthorized(new{detail ="Invalid username or password."});
    }
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if(Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new UserProfileDto("System Admin","Admin"));
        }
        return Unauthorized(new { detail = "Session expired or missing authentication cookie."});
    }
    
}