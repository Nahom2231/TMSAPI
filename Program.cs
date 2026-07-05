using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using TmsApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services;
using TmsApi.Controllers;
using System.Text.RegularExpressions;
using Tms.Api.Persistence;
using TmsApi.Filters;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi(); 
// Register enrollment service. Use the concrete implementation name 'EnrollmentService'
// (some projects name the implementation in plural). If your implementation class
// is named differently, adjust the type accordingly.
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>(); 


builder.Services.AddScoped<ICourseService, CourseService>();
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

  builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging()); 
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>();
if (app.Environment.IsDevelopment())
{
 app.MapOpenApi();   
 app.MapScalarApiReference();
}

    


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
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
if(app.Environment.IsDevelopment())
{
   using var scope = app.Services.CreateScope();
   var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
   await DataSeeder.SeedAsync(context); 
}
app.Run();
public record EnrollmentRecord(string StudentId, string CourseCode, DateTime EnrolledAt);
