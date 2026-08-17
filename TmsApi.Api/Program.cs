#pragma warning disable EXTEXP0018
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using TmsApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;

using TmsApi.Infrastructure.Workers;
using TmsApi.Application.Services;
using TmsApi.Controllers;
using System.Text.RegularExpressions;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Filters;
using Asp.Versioning;
using TmsApi.Middleware;
using Microsoft.Extensions.Options;
using FluentValidation;
using TmsApi.ExceptionHandlers;
using TmsApi.Enrollments.Commands;
using TmsApi.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using Microsoft.Extensions.Caching;
using TmsApi.Infrastructure.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using TmsApi.Api.RateLimiting;
using System.Security.Principal;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Application.Hubs;
using System.Security.Cryptography.X509Certificates;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.IO.Pipes;
using System.Runtime.Serialization;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.ExternalServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.NpgSql;
using System.Diagnostics.Tracing;
using System.Data.Common;
using System.CodeDom.Compiler;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.AspNetCore.Antiforgery;
using System.Security;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);
        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });
    options.AddConcurrencyLimiter("transcripts", options =>
    {
        options.PermitLimit =5;
        options.QueueLimit =20;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHybridCache(options=>
{
options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
{
 Expiration = TimeSpan.FromMinutes(10),

LocalCacheExpiration = TimeSpan.FromMinutes(2)
 };
});
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
      FullMode = BoundedChannelFullMode.Wait  
    }));
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion= new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified=true;
    options.ReportApiVersions=true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddOpenApi(); 
// Register enrollment service. Use the concrete implementation name 'EnrollmentService'
// (some projects name the implementation in plural). If your implementation class
// is named differently, adjust the type accordingly.
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>(); 


builder.Services.AddScoped<ICourseService, CourseService>();
 builder.Services.AddProblemDetails();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters=new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
          ValidateIssuer=true,
          ValidateAudience = true,
          ValidateLifetime=true,
          ValidIssuer="https://localhost:7295",
          ValidAudience="https://localhost:7295",
          IssuerSigningKey= new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
              System.Text.Encoding.UTF8.GetBytes("YourSuperSecretkeyThatisLongEnough123")
          )

        };
    });
    // Register FluentValidation validators (removed AddValidatorsFromAssemblyContaining usage
    // to avoid extension method resolution issues). Register validators explicitly if needed.
     builder.Services.AddMediatR(cfg =>
{
    // This scans the current running assembly (TmsApi) directly
    cfg.RegisterServicesFromAssembly(typeof(TmsApi.Application.Queries.GetCoursesHandler).Assembly);
    
    //cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    //cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
  builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()); 
builder.Services.AddAuthorization();
builder.Services.AddScoped<TmsApi.Application.Common.ITmsDbContext>(provider => provider.GetRequiredService<TmsDbContext>());
builder.Services.AddHostedService<TranscriptWorker>();  
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Allow Angular", policy =>
    {
     policy.WithOrigins("http://localhost:4200")
               .AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // allows local browser test
              .AllowCredentials();
    });
});
 builder.Services.AddResiliencePipeline("certificate-api", pipeline =>
 {
     pipeline.AddTimeout(TimeSpan.FromSeconds(5))

     .AddCircuitBreaker(new CircuitBreakerStrategyOptions
     {
         FailureRatio = 0.5,
         MinimumThroughput = 10,
         SamplingDuration = TimeSpan.FromSeconds(30),
         BreakDuration = TimeSpan.FromSeconds(15),
         ShouldHandle = new PredicateBuilder()

            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>(),
        OnOpened = args=>
        {
            Console.WriteLine("Circuit OPENED- stopping requests to certificate Service");
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            Console.WriteLine("Circuit CLOSED - certificate service recovered");
            return ValueTask.CompletedTask;
        }
     })
     .AddRetry(new RetryStrategyOptions
     {
         MaxRetryAttempts=3,
         Delay = TimeSpan.FromMilliseconds(500),
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true,
         ShouldHandle = new PredicateBuilder()
         .Handle<HttpRequestException>()
         .Handle<TimeoutRejectedException>(),
         OnRetry = args =>
         {
             Console.WriteLine($"Retry #{args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds:FO}ms ({args.Outcome.Exception?.GetType().Name})");
             return ValueTask.CompletedTask;
         }
     });
 
     
 });
 
 builder.Services.AddHttpClient<ICertificateService, CertificateService>((sp, client) =>
 {
     var baseUrl = sp.GetRequiredService<IConfiguration>().GetValue<string>("TmsApi:PublicBaseUrl")
     ?? "https://localhost:5029";
     client.BaseAddress = new Uri(baseUrl);
 });
 builder.Services.AddHealthChecks()
 .AddCheck("self", ()=>HealthCheckResult.Healthy("alive"), tags: ["live"])
 .AddNpgSql(
     builder.Configuration.GetConnectionString("DefaultConnection")!,
    name: "postgres",
    tags: ["ready"]);
 builder.Logging.AddJsonConsole(options =>
 {
     options.IncludeScopes = true;
     options.JsonWriterOptions = new() { Indented= false};
 });
 const string ServiceName = "tms-api";

 builder.Services.AddOpenTelemetry()
 .ConfigureResource(r => r.AddService(
    serviceName: ServiceName,
    serviceVersion: "1.0.0"))
    .WithTracing(t => t
    .AddSource(ServiceName)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter())
   .WithMetrics(m=> m
   .AddMeter(ServiceName)
   .AddAspNetCoreInstrumentation()
   .AddHttpClientInstrumentation()
   .AddRuntimeInstrumentation()
   .AddOtlpExporter());
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();
app.UseStatusCodePages();
app.UseExceptionHandler();
// Request logging middleware removed because the type was not available in this project.
// If you add a RequestLoggingMiddleware implementation, re-enable the line below:
// app.UseMiddleware<RequestLoggingMiddleware>();
if (app.Environment.IsDevelopment())
{
 app.MapOpenApi();   
 app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
});
}
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check =>check.Tags.Contains("live")
}).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate= check => check.Tags.Contains("ready")
}).DisableRateLimiting();
    
app.UseExceptionHandler();

app.UseRouting();
app.UseCors("TmsClient");
app.UseCors("Allow Angular");
//app.UseRateLimiter();
app.UseMiddleware<TmsApi.Middleware.V1DepreciationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next)=>
{
    if (context.User.Identity?.IsAuthenticated==true|| context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);

        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        new CookieOptions
        {
            HttpOnly = false,
            Secure =!app.Environment.IsDevelopment(),
            SameSite= SameSiteMode.Strict
        });
    }
    await next (context);
});
app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");
using (var scope=app.Services.CreateScope())
{
    var context=scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    //context.Database.Migrate();
    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
        new() {RegistrationNumber= "TMS-2026-0001", Name = "Alice Smith", 
        GPA= 3.8M, IsActive= true},
        new() {RegistrationNumber= "TMS-2026-0002", Name = "Bob Ones",
        GPA= 2.9M, IsActive=true},
        new() {RegistrationNumber= "TMS-2026-0003", Name = "Charlie Brown",
        GPA= 3.4M, IsActive=false},
        new() {RegistrationNumber= "TMS-2026-0004", Name = "Diana Prince",
        GPA= 3.9M, IsActive=true},
        new() {RegistrationNumber= "TMS-2026-0005", Name = "Evan Wright",
        GPA= 2.5M, IsActive=true}
        };
        context.Set<Student>().AddRange(students);

        var courses = new List<Course>
        {
            new() { Code="CS-101",  Title = "Introduction to Computer Science", MaxCapacity =30 },
            new() { Code="CS-201",  Title = "Data Structures and Algorithm", MaxCapacity =25 },
            new() { Code="ENG102", Title = "Calculus I", MaxCapacity =40 }
            
        };
        context.Set<Course>().AddRange(courses);
        context.SaveChanges();
         var enrollments = new List<Enrollment>
        {
          new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
          new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
          new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
          new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Set<Enrollment>().AddRange(enrollments);
        context.SaveChanges();
    }
}

// try
// {
//     var course = new TmsApi.Entities.Course {Code= "CS-101", Title = "C# Basics", MaxCapacity=10 };
//     course.Status = TmsApi.Entities.CourseStatus.Archived;

//     if(course.MaxCapacity!=0)
//      Console.WriteLine("FAIL: Capacity must drop to 0 when archived");
//      else
//      Console.WriteLine("PASS: Archived course has zero capacity");
// }
// catch(Exception ex)
// {
//     Console.WriteLine($"Challenge 1: UNExPECTED FAILURE - {ex.Message}");
// }

var records = new List<EnrollmentRecord>
{
    new("S1", "CS1", DateTime.UtcNow),
    new("S1", "CS2", DateTime.UtcNow),
    new("S3", "CS1", DateTime.UtcNow),
    new("S3", "CS1", DateTime.UtcNow),
    new("S3", "CS1", DateTime.UtcNow),
    new("S4", "CS1", DateTime.UtcNow),
    new("S4", "CS2", DateTime.UtcNow),
};

var testStudents = new List<Student>
{
    new Student { RegistrationNumber = "S1", Name = "Abebe", Age = 22, GPA = 3.9m },
    new Student { RegistrationNumber = "S2", Name = "Kidane", Age = 21, GPA = 2.4m },
    new Student { RegistrationNumber = "S3", Name = "Dawit", Age = 19, GPA = 3.7m },
    new Student { RegistrationNumber = "S4", Name = "Sara", Age = 23, GPA = 3.6m },
};

var report = testStudents
    .Select(s => new
    {
        Student = s,
        Count = records.Count(r => r.StudentId == s.RegistrationNumber)
    })
    .Where(x => x.Student.Age >= 20 && x.Student.GPA >= 3.0m && x.Count >= 2)
    .Select(x => new
    {
        Name = x.Student.Name,
        GPA = x.Student.GPA,
        EnrollmentCount = x.Count
    })
    .GroupBy(s => s.GPA >= 3.8m ? "High Honors" : s.GPA >= 3.5m ? "Honors" : "Dean's List")
    .OrderBy(g => g.Key)
    .Select(g => new
    {
        Brand = g.Key,
        Students = g.OrderByDescending(s => s.GPA).ToList()
    })
    .ToList();

    Console.WriteLine("--- GRANT ELIGIBILITY REPORT");

    foreach (var group in report)
{
    Console.WriteLine($"  {group.Brand}");
    foreach (var item in group.Students)
    {
        Console.WriteLine($"    {item.Name} ({item.GPA}) - {item.EnrollmentCount} enrollments");
    }
}
var attempts=0;
app.MapPost("/fake/certificates", async () =>
{
    var n = Interlocked.Increment(ref attempts);
    if (n % 7 == 0)
    {
        await Task.Delay(TimeSpan.FromSeconds(20));
        return Results.Ok(new {Status = "issued", Attempts = n });


    }
    if(n%3 != 0)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    if (n % 11 == 0)
    {
        return Results.BadRequest(new {error = "validation_failed " });
    }
    return Results.Ok(new {Status = "issued", Attempt = n });
}).WithTags("Lab-fixtures");

// if(app.Environment.IsDevelopment())
// {
//    using var scope = app.Services.CreateScope();
//    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
//    await DataSeeder.SeedAsync(context); 
//}
var cryptoService = new CryptoDemoService();
string hash1= cryptoService.HashUserPassword("Password123!");
string hash2= cryptoService.HashUserPassword("Password123!");

Console.WriteLine($"[BCrypt Demo] Hash 1: {hash1}");
Console.WriteLine($"[BCrypt Demo] Hash 2: {hash2}");
Console.WriteLine($"[BCrypt Demo]  Match 1: {cryptoService.VerifyUserPassword("Password123!", hash1 )}");
Console.WriteLine($"[BCrypt Demo]  Match 2:  {cryptoService.VerifyUserPassword("Password123!", hash2)}");


app.Run();
public record EnrollmentRecord(string StudentId, string CourseCode, DateTime EnrolledAt);