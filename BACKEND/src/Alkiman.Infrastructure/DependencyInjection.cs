using Alkiman.Application.AssetBlocks;
using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Categories;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Application.Landlords;
using Alkiman.Application.Payments;
using Alkiman.Application.Rentals;
using Alkiman.Infrastructure.Persistence;
using Alkiman.Infrastructure.Persistence.TypeHandlers;
using Alkiman.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Alkiman.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra el acceso a datos (Dapper + SQL Server) y sus repositorios.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        DapperTypeHandlerRegistration.RegisterAll();

        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<ILandlordRepository, LandlordRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetBlockRepository, AssetBlockRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
