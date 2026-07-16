using Alkiman.Application.AssetBlocks;
using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Categories;
using Alkiman.Application.Customers;
using Alkiman.Application.Landlords;
using Alkiman.Application.Payments;
using Alkiman.Application.Rentals;
using Microsoft.Extensions.DependencyInjection;

namespace Alkiman.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios de casos de uso de la capa Application (sin infraestructura).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILandlordService, LandlordService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetBlockService, AssetBlockService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRentalService, RentalService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
