# Module 4 Lab Session 2: Services Done Right

| Field | Value |
| :--- | :--- |
| **Module** | M4 ASP.NET Core 10 Fundamentals |
| **Exercises** | 2 (Captive Dependencies), 3 (Options Pattern), 4 (Structured Logging) |
| **Assessment Tiers** | Tier 2 DI (LO 4.3, 4.4) + Tier 3 Config (LO 4.5) + Tier 4 Logging (LO 4.6) |

---

## Prerequisites Check Before You Start

Your `TmsApi` project must contain:
* [ ] `Program.cs` with the canonical pipeline order from Session 1 (`UseRouting`, `UseAuthentication`, `UseAuthorization`, all before any `MapGet`).
* [ ] `RequestLoggingMiddleware.cs` registered as the first middleware in the pipeline.
* [ ] A working `GET /api/assessments/results` endpoint that returns `401 Unauthorized` for anonymous calls.
* [ ] An `X-Correlation-Id` header on every response.

> **Note:** If any of these are missing or incomplete, go back to Session 1. The three exercises in this session depend on a working pipeline and a Correlation ID. Without them, you will spend time chasing symptoms instead of learning the lesson.

---

## What This Session Is Really About

Session 2 covers three topics that look unrelated until you see them break in production:
1. **DI lifetimes done wrong (Exercise 2):** A singleton holds a scoped service, causing the database connection pool to exhaust at 3 AM.
2. **Configuration done wrong (Exercise 3):** A required setting is missing, the app starts fine, then the first user to trigger that code path crashes the request.
3. **Logging done wrong (Exercise 4):** The logs work, but they are unsearchable strings, so when something goes wrong you cannot find the one log line that matters.

These are the silent-failure topics of ASP.NET Core. None of them produce a compile error, and none fail in development with a single user. They fail at 2 AM under real load with a real customer waiting. Your job in Session 2 is to learn the discipline that prevents each of them—DI lifetime correctness, validated configuration, and structured logging—as one cohesive arc, because in real systems they fail together.

---

## TMS Service Foundation

Before you continue, you need the enrollment service that the next exercises depend on. This is the same TMS domain from M1, now exposed as a service the web layer can inject.

Create a new file called **`EnrollmentService.cs`**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

//--- The contract ---
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

//--- The in-memory implementation ---
public class EnrollmentService : IEnrollmentService
{
    private readonly Dictionary<string, EnrollmentRecord> _store = new();
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store[id] = record;
        
        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}", 
            studentId, courseCode, id);
            
        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        return Task.FromResult(removed);
    }
}

//--- The data shape ---
public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);
