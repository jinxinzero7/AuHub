using Payment.Application;
using Payment.Application.Commands.TopUpWallet;
using Payment.Infrastructure;
using Payment.Infrastructure.Data;
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

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Application
builder.Services.AddApplication();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<TopUpWalletCommand>();

// Health Checks
builder.Services.AddHealthChecks();

// FastEndpoints
builder.Services.AddFastEndpoints();

// JWT Authentication
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AuHub-Users";
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("JWT Secret not configured");

    jwtSecret = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Payment Service API";
        s.Version = "v1";
        s.Description = "Payment and Wallet Service for AuHub";
    };
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.Migrate();
}

app.UseCorrelationId();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.MapHealthChecks("/health");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Payment API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
