using Identity.Application;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Commands.Auth.RefreshToken;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Identity.API.Endpoints.Auth;
using Identity.API.Middleware;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuHub.Shared.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.WithCorrelationId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginCommandValidator>();

// Health Checks
builder.Services.AddHealthChecks();

// FastEndpoints
builder.Services.AddFastEndpoints();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Identity Service API";
        s.Version = "v1";
        s.Description = "Identity and authentication microservice with JWT";
    };
});

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    dbContext.Database.Migrate();
}

// Configure pipeline
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Ban check middleware (must be after auth)
app.UseMiddleware<BanMiddleware>();

app.UseFastEndpoints();

app.MapHealthChecks("/health");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
