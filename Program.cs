using Microsoft.AspNetCore.Authentication.JwtBearer;
using TmsApi;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddControllers();
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

    builder.Host.UseDefaultServiceProvider(options=>{
        options.ValidateScopes= true;
        options.ValidateOnBuild=true;
    });

builder.Services.AddAuthorization();
var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler("/error");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});


app.MapGet("/api/assessments/results", () => Results.Ok(new
{
   courseCode = "CS-101",
   studentId="S-001",
   letterGrade="A" 
})).RequireAuthorization(); 
app.MapControllers();
app.Run();




