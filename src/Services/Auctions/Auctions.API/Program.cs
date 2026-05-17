using Auctions.Application;
using Auctions.Application.Commands.CreateLot;
using Auctions.Application.Commands.PlaceBid;
using Auctions.Application.Services;
using Auctions.Infrastructure;
using Auctions.Infrastructure.Data;
using Auctions.Infrastructure.Storage;
using Auctions.API.Endpoints.Lots;
using Auctions.API.Hubs;
using Auctions.API.SignalR;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Minio;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateLotCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PlaceBidCommandValidator>();

// SignalR Event Publisher
builder.Services.AddScoped<IEventPublisher, SignalREventPublisher>();

// MinIO Client
var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "minio:9000";
var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "auhub-minio-admin";
var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "AuHub_MinIO_2026_Secure!";
var minioBucket = builder.Configuration["MinIO:BucketName"] ?? "auhub-lots";
var minioWithSSL = bool.Parse(builder.Configuration["MinIO:WithSSL"] ?? "false");

var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(minioWithSSL)
    .Build();

builder.Services.AddSingleton<IMinioClient>(minioClient);
builder.Services.AddScoped<IImageStorageService>(_ => new MinioImageStorageService(minioClient, minioBucket));

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

// CORS for Frontend + SignalR
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
        s.Title = "AuctionHub API";
        s.Version = "v1";
        s.Description = "Microservices-based auction platform with JWT authentication";
    };
});

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();

    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }

    // Initialize MinIO bucket
    var imageStorage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
    await imageStorage.InitializeBucketAsync();
}

// Configure pipeline
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints();

// Map SignalR Hub
app.MapHub<AuctionHub>("/hubs/auction");

app.Run();
