using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TmsUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            // Ensure DB schema is ready
            await context.Database.EnsureCreatedAsync();

            try
            {
                await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Courses\" ADD COLUMN IF NOT EXISTS \"InstructorId\" text;");
            }
            catch
            {
                // Non-Postgres or already migrated
            }

            // 1. Seed Roles
            string[] roles = ["Admin", "Instructor", "Student"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Seeded role: {Role}", role);
                }
            }

            // 2. Seed Default Users
            var defaultUsers = new[]
            {
                (Email: "admin@tms.local", Password: "Admin123456!#", FirstName: "Admin", LastName: "Superuser", Role: "Admin"),
                (Email: "instructor@tms.local", Password: "Instructor123456!#", FirstName: "Leul", LastName: "Kebede", Role: "Instructor"),
                (Email: "student@tms.local", Password: "Student123456!#", FirstName: "Alice", LastName: "Smith", Role: "Student")
            };

            foreach (var u in defaultUsers)
            {
                var existing = await userManager.FindByEmailAsync(u.Email);
                if (existing == null)
                {
                    var user = new TmsUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, u.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, u.Role);
                        logger.LogInformation("Seeded user: {Email} ({Role})", u.Email, u.Role);
                    }
                }
            }

            // 3. Seed Students
            if (!await context.Students.AnyAsync())
            {
                var students = new List<Student>
                {
                    new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", Age = 21, GPA = 3.85m, IsActive = true },
                    new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", Age = 22, GPA = 3.20m, IsActive = true },
                    new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", Age = 20, GPA = 2.95m, IsActive = true },
                    new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", Age = 23, GPA = 3.92m, IsActive = true },
                    new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", Age = 22, GPA = 3.50m, IsActive = true }
                };
                await context.Students.AddRangeAsync(students);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} students", students.Count);
            }

            // 4. Seed Courses
            if (!await context.Courses.AnyAsync())
            {
                var courses = new List<Course>
                {
                    new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30, Status = CourseStatus.Active },
                    new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25, Status = CourseStatus.Active },
                    new() { Code = "CS-301", Title = "Database Systems & SQL Optimization", MaxCapacity = 30, Status = CourseStatus.Active },
                    new() { Code = "CS-401", Title = "Advanced Web Architecture & APIs", MaxCapacity = 20, Status = CourseStatus.Active },
                    new() { Code = "ENG-102", Title = "Calculus & Applied Mathematics", MaxCapacity = 40, Status = CourseStatus.Active }
                };
                await context.Courses.AddRangeAsync(courses);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} courses", courses.Count);
            }

            // 5. Seed Enrollments
            if (!await context.Enrollments.AnyAsync())
            {
                var allStudents = await context.Students.ToListAsync();
                var allCourses = await context.Courses.ToListAsync();

                if (allStudents.Count >= 4 && allCourses.Count >= 3)
                {
                    var enrollments = new List<Enrollment>
                    {
                        new() { StudentId = allStudents[0].Id, CourseId = allCourses[0].Id, Grade = 92.5m, EnrolledAt = DateTime.UtcNow.AddMonths(-3) },
                        new() { StudentId = allStudents[0].Id, CourseId = allCourses[1].Id, Grade = 88.0m, EnrolledAt = DateTime.UtcNow.AddMonths(-2) },
                        new() { StudentId = allStudents[1].Id, CourseId = allCourses[0].Id, Grade = 76.5m, EnrolledAt = DateTime.UtcNow.AddMonths(-3) },
                        new() { StudentId = allStudents[1].Id, CourseId = allCourses[2].Id, Grade = 81.0m, EnrolledAt = DateTime.UtcNow.AddMonths(-1) },
                        new() { StudentId = allStudents[2].Id, CourseId = allCourses[0].Id, Grade = 68.0m, EnrolledAt = DateTime.UtcNow.AddMonths(-3) },
                        new() { StudentId = allStudents[3].Id, CourseId = allCourses[1].Id, Grade = 95.0m, EnrolledAt = DateTime.UtcNow.AddMonths(-2) },
                        new() { StudentId = allStudents[3].Id, CourseId = allCourses[3].Id, Grade = 91.5m, EnrolledAt = DateTime.UtcNow.AddMonths(-1) },
                        new() { StudentId = allStudents[4].Id, CourseId = allCourses[2].Id, Grade = 84.0m, EnrolledAt = DateTime.UtcNow.AddMonths(-1) }
                    };
                    await context.Enrollments.AddRangeAsync(enrollments);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} enrollments", enrollments.Count);
                }
            }

            // 6. Seed Assessments
            if (!await context.Assessments.AnyAsync())
            {
                var allCourses = await context.Courses.ToListAsync();
                var assessments = new List<Assessment>();

                foreach (var course in allCourses)
                {
                    assessments.Add(new Assessment { CourseId = course.Id, Title = $"{course.Code} - Midterm Exam", MaxScore = 100m, Weight = 0.35m });
                    assessments.Add(new Assessment { CourseId = course.Id, Title = $"{course.Code} - Final Practical Project", MaxScore = 100m, Weight = 0.50m });
                    assessments.Add(new Assessment { CourseId = course.Id, Title = $"{course.Code} - Quizzes & Participation", MaxScore = 100m, Weight = 0.15m });
                }

                await context.Assessments.AddRangeAsync(assessments);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} assessments", assessments.Count);
            }

            // 7. Seed Certificates
            if (!await context.Certificates.AnyAsync())
            {
                var allStudents = await context.Students.ToListAsync();
                var allCourses = await context.Courses.ToListAsync();

                if (allStudents.Any() && allCourses.Any())
                {
                    var certificates = new List<Certificate>
                    {
                        new()
                        {
                            SerialNumber = "CERT-2026-A19F8C",
                            StudentId = allStudents[0].Id,
                            CourseId = allCourses[0].Id,
                            IssuedAt = DateTime.UtcNow.AddDays(-15)
                        },
                        new()
                        {
                            SerialNumber = "CERT-2026-B84E2D",
                            StudentId = allStudents[3].Id,
                            CourseId = allCourses[1].Id,
                            IssuedAt = DateTime.UtcNow.AddDays(-10)
                        }
                    };
                    await context.Certificates.AddRangeAsync(certificates);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} certificates", certificates.Count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Data seeding encountered an issue, proceeding with startup: {Message}", ex.Message);
        }
    }
}
