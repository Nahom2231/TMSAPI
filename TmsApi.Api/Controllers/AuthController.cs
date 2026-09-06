using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Authentication")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TmsDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TmsDbContext context,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record RegisterRequest(string Email, string Password, string FirstName, string? Role);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NewPassword);

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) 
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized
            });

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var secondsRemaining = lockoutEnd.HasValue 
                ? (int)Math.Max(1, (lockoutEnd.Value.UtcDateTime - DateTime.UtcNow).TotalSeconds)
                : 60;

            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Title = "Account Locked",
                Detail = $"Account locked due to multiple failed login attempts. Please wait {secondsRemaining} seconds.",
                Status = StatusCodes.Status423Locked,
                Extensions = { ["lockoutSeconds"] = secondsRemaining }
            });
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
                {
                    Title = "Account Locked",
                    Detail = "Account has been locked for 60 seconds due to repeated failed login attempts.",
                    Status = StatusCodes.Status423Locked,
                    Extensions = { ["lockoutSeconds"] = 60 }
                });
            }

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var remainingAttempts = Math.Max(0, 6 - failedCount);

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = remainingAttempts > 0 
                    ? $"Invalid email or password. {remainingAttempts} attempt(s) remaining before temporary lockout."
                    : "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = { ["failedCount"] = failedCount, ["remainingAttempts"] = remainingAttempts }
            });
        }
        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateJwt(user, roles);

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                roles
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid Refresh Token",
                Detail = "The provided refresh token was not found.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId)
                .ToListAsync();

            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await _context.SaveChangesAsync();

            return Unauthorized(new ProblemDetails
            {
                Title = "Token Expired or Revoked",
                Detail = "Refresh token is no longer valid. Please log in again.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        storedToken.IsUsed = true;

        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateJwt(user, roles);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Email and password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Enforce password policy: min 8 characters, at least one uppercase letter, at least one digit
        if (request.Password.Length < 8 || !request.Password.Any(char.IsUpper) || !request.Password.Any(char.IsDigit))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Password Policy Violation",
                Detail = "Password must be at least 8 characters long and contain at least one uppercase letter and one digit.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "User Already Exists",
                Detail = $"A user with email '{request.Email}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = string.Join("; ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        var targetRole = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role;
        if (!await _roleManager.RoleExistsAsync(targetRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(targetRole));
        }
        await _userManager.AddToRoleAsync(user, targetRole);

        return Ok(new { message = "User registered successfully.", email = user.Email, role = targetRole });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Email is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Security best practice: Do not leak user existence, return friendly success
            return Ok(new 
            { 
                message = "If an account exists with this email, password reset instructions have been generated.", 
                email = request.Email 
            });
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        return Ok(new
        {
            message = "Password reset instructions and token generated successfully.",
            email = user.Email,
            resetToken // Provided for direct verification / development flow
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Email, reset token, and new password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Enforce password policy
        if (request.NewPassword.Length < 8 || !request.NewPassword.Any(char.IsUpper) || !request.NewPassword.Any(char.IsDigit))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Password Policy Violation",
                Detail = "Password must be at least 8 characters long and contain at least one uppercase letter and one digit.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = "No user found with the specified email address.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Password Reset Failed",
                Detail = string.Join("; ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Clear any active lockout and failed counter
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new { message = "Password has been successfully reset. You may now log in with your new password." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            roles
        });
    }
}
