using Auctions.Application;
using Auctions.Application.Commands.CreateLot;
using Auctions.Application.Commands.PlaceBid;
using Auctions.Application.Options;
using Auctions.Application.Services;
using Auctions.Infrastructure;
using Auctions.Infrastructure.Data;
using Auctions.Infrastructure.Services;
using Auctions.Infrastructure.Storage;
using Auctions.API.Endpoints.Lots;
using Auctions.API.Hubs;
using Auctions.API.SignalR;
using Auctions.Domain.Entities;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Minio;
using System.Text;
using AuHub.Shared.Middleware;
using AuHub.Shared.ValueObjects;
using MassTransit;
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
builder.Services.Configure<ExternalUrlOptions>(builder.Configuration.GetSection(ExternalUrlOptions.SectionName));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateLotCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<PlaceBidCommandValidator>();

// SignalR
builder.Services.AddSignalR();

// SignalR Event Publisher
builder.Services.AddScoped<IEventPublisher, SignalREventPublisher>();

// Notifications HTTP Client
var notificationsBaseUrl = builder.Configuration["Notifications:BaseUrl"] ?? "http://notifications-api:8080";
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.BaseAddress = new Uri(notificationsBaseUrl);
});

// Payment HTTP Client
var paymentBaseUrl = builder.Configuration["Payment:BaseUrl"] ?? "http://payment-api:8080";
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
{
    client.BaseAddress = new Uri(paymentBaseUrl);
});

// MinIO Client
var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "minio:9000";
var minioExternalEndpoint = builder.Configuration["MinIO:ExternalEndpoint"] ?? "http://localhost:9000";
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
builder.Services.AddScoped<IImageStorageService>(_ => new MinioImageStorageService(minioClient, minioBucket, minioExternalEndpoint, minioAccessKey, minioSecretKey));

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

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auctions", false));

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
    dbContext.Database.Migrate();

    // Seed demo data if no lots exist
    if (!dbContext.Lots.Any())
    {
        var sellerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bidderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var lot1 = Lot.Create("Золотая монета 10 рублей 1899 года", "Редкая золотая монета Российской Империи в отличном состоянии. Сохранность XF.", Money.FromDecimal(50000), TimeSpan.FromDays(3), sellerId);
        var lot2 = Lot.Create("Картина «Закат над Волгой», масло, холст", "Оригинальная работа современного художника. Размер 60x80 см. Оформлена в багет.", Money.FromDecimal(25000), TimeSpan.FromDays(1), sellerId);
        var lot3 = Lot.Create("Коллекция из 50 советских значков", "Политические и памятные значки 1960-1980-х годов. Все в хорошем состоянии.", Money.FromDecimal(3000), TimeSpan.FromDays(2), sellerId);
        var lot4 = Lot.Create("Антикварный письменный прибор, бронза", "Чернильница с подсвечником, Франция, конец XIX века. Патина, гравировка.", Money.FromDecimal(15000), TimeSpan.FromDays(5), sellerId);
        var lot5 = Lot.Create("Серебряный набор столовых приборов (12 предметов)", "Серебро 925 пробы, СССР, 1950-е годы. В оригинальном футляре.", Money.FromDecimal(35000), TimeSpan.FromDays(7), sellerId);

        lot1.Approve(); lot1.Publish();
        lot2.Approve(); lot2.Publish();
        lot3.Approve(); lot3.Publish();
        lot4.Approve(); lot4.Publish();
        // lot5 stays Draft

        dbContext.Lots.AddRange(lot1, lot2, lot3, lot4, lot5);
        await dbContext.SaveChangesAsync();

        dbContext.Bids.AddRange(
            Bid.Create(lot1.Id, bidderId, Money.FromDecimal(52000)),
            Bid.Create(lot1.Id, bidderId, Money.FromDecimal(55000)),
            Bid.Create(lot2.Id, bidderId, Money.FromDecimal(26000)),
            Bid.Create(lot2.Id, bidderId, Money.FromDecimal(28500)),
            Bid.Create(lot3.Id, bidderId, Money.FromDecimal(3500)),
            Bid.Create(lot3.Id, bidderId, Money.FromDecimal(4200))
        );
        await dbContext.SaveChangesAsync();
    }

    // Initialize MinIO bucket
    var imageStorage = scope.ServiceProvider.GetRequiredService<IImageStorageService>();
    await imageStorage.InitializeBucketAsync();
}

// Configure pipeline
app.UseCorrelationId();

app.UseCors();

// Enable request buffering to support file uploads
app.Use((context, next) =>
{
    context.Request.EnableBuffering();
    return next();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseWhen(ctx => !ctx.Request.HasFormContentType, appBuilder =>
    {
        appBuilder.UseSwaggerGen();
    });
}

// Map SignalR Hub
app.MapHub<AuctionHub>("/hubs/auction");

app.MapHealthChecks("/health");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Auctions API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
