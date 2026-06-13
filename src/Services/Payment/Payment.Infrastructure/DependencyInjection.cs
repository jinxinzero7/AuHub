using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Repositories;
using Payment.Application.Services;
using Payment.Infrastructure.Data;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPaymentProvider, DemoPaymentProvider>();
        services.AddScoped<IPaymentCheckoutProvider>(_ => new RobokassaPaymentCheckoutProvider(new RobokassaOptions
        {
            MerchantLogin = configuration["PaymentProviders:Robokassa:MerchantLogin"] ?? string.Empty,
            Password1 = configuration["PaymentProviders:Robokassa:Password1"] ?? string.Empty,
            Password2 = configuration["PaymentProviders:Robokassa:Password2"] ?? string.Empty,
            PaymentUrl = configuration["PaymentProviders:Robokassa:PaymentUrl"] ?? "https://auth.robokassa.ru/Merchant/Index.aspx",
            Culture = configuration["PaymentProviders:Robokassa:Culture"] ?? "ru",
            IsTest = bool.Parse(configuration["PaymentProviders:Robokassa:IsTest"] ?? "true")
        }));

        return services;
    }
}
