using Identity.Domain.Interfaces;
using Identity.Application.Services;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPhoneVerificationCodeRepository, PhoneVerificationCodeRepository>();
        services.AddScoped<IDocumentVerificationRequestRepository, DocumentVerificationRequestRepository>();
        services.AddScoped<IEmailVerificationSender, DevEmailVerificationSender>();
        services.AddScoped<IPhoneVerificationSender, DevPhoneVerificationSender>();
        services.AddSingleton<IDocumentStorageService>(_ =>
        {
            var minioEndpoint = configuration["MinIO:Endpoint"] ?? "minio:9000";
            var minioAccessKey = configuration["MinIO:AccessKey"] ?? "auhub-minio-admin";
            var minioSecretKey = configuration["MinIO:SecretKey"] ?? "AuHub_MinIO_2026_Secure!";
            var minioBucket = configuration["MinIO:DocumentBucketName"] ?? "auhub-documents";
            var minioWithSsl = bool.Parse(configuration["MinIO:WithSSL"] ?? "false");

            var client = new MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .WithSSL(minioWithSsl)
                .Build();

            return new MinioDocumentStorageService(client, minioBucket);
        });

        return services;
    }
}
