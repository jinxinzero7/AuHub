using Notifications.Application;
using Notifications.Application.Commands.SendNotification;
using Notifications.Application.Commands.MarkAsRead;
using Notifications.Application.Queries.GetUserNotifications;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Data;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuHub.Shared.Middleware;
using MassTransit;
using Notifications.API.Consumers;
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
builder.Services.AddValidatorsFromAssemblyContaining<SendNotificationCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<GetUserNotificationsQueryValidator>();

// Health Checks
builder.Services.AddHealthChecks();

// FastEndpoints
builder.Services.AddFastEndpoints();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notifications", false));

    x.AddConsumer<BidPlacedConsumer>();
    x.AddConsumer<AuctionCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMQ:User"] ?? "auhub";
        var pass = builder.Configuration["RabbitMQ:Password"] ?? "AuHub_Rabbit_2026!";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.PrefetchCount = 20;

        cfg.ConfigureEndpoints(context);
    });
});

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

// CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://auhub.yourdomain.com")
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
        s.Title = "Notifications Service API";
        s.Version = "v1";
        s.Description = "Notifications microservice for AuHub auction platform";
    };
});

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    dbContext.Database.Migrate();
}

// Configure pipeline
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
    Log.Fatal(ex, "Notifications API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
