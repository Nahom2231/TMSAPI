using Microsoft.AspNetCore.Authentication.JwtBearer;

using System.Text.Json;
using TmsApi;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
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
app.Run();

