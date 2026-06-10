using Microsoft.AspNetCore.Authentication.JwtBearer;
var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthorization();
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new { message = "Protected data" });
}).RequireAuthorization(); 

app.Run();