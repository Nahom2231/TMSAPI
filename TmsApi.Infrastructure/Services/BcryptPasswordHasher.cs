using System;
using Microsoft.AspNetCore.Identity;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher<TmsUser>
{
    public string HashPassword(TmsUser user, string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public PasswordVerificationResult VerifyHashedPassword(TmsUser user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        // Check if string matches standard BCrypt prefix ($2a$, $2b$, $2y$, $2x$)
        if (hashedPassword.StartsWith("$2"))
        {
            try
            {
                var isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
                return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        // Graceful fallback: Check with default ASP.NET Core Identity PasswordHasher (e.g. for existing seeded users)
        var fallbackHasher = new PasswordHasher<TmsUser>();
        var fallbackResult = fallbackHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        if (fallbackResult != PasswordVerificationResult.Failed)
        {
            // Upgrades legacy hash to BCrypt on next save
            return PasswordVerificationResult.SuccessRehashNeeded;
        }

        return PasswordVerificationResult.Failed;
    }
}
