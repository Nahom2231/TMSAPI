using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Collections.Generic;
using System.Text.Json;
using TmsApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
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
app.UseMiddleware<RequestLoggingMiddleware>();
if (app.Environment.IsDevelopment())
{
 app.MapOpenApi();   
 app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
}
    

app.UseStatusCodePages();
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
            new() { Code="CS-101", Name="CS-101", Title = "Introduction to Computer Science", Capacity =30 },
            new() { Code="CS-201", Name="CS-201", Title = "Data Structures and Algorithm", Capacity =25 },
            new() { Code="ENG102", Name="ENG102", Title = "Calculus I", Capacity =40 }
            
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
app.Run();

