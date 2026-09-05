using Alkiman.Application.AssetBlocks;
using Alkiman.Application.AssetGroups;
using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Auth;
using Alkiman.Application.Carwash;
using Alkiman.Application.Categories;
using Alkiman.Application.ContractTemplates;
using Alkiman.Application.Contracts;
using Alkiman.Application.Customers;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Locations;
using Alkiman.Application.Me;
using Alkiman.Application.Modules;
using Alkiman.Application.MyRental;
using Alkiman.Application.Payments;
using Alkiman.Application.Permissions;
using Alkiman.Application.Portal;
using Alkiman.Application.PortalLinks;
using Alkiman.Application.RentalImports;
using Alkiman.Application.RentalRequests;
using Alkiman.Application.Rentals;
using Alkiman.Application.Reminders;
using Alkiman.Application.Reports;
using Alkiman.Application.Roles;
using Alkiman.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Alkiman.Application;

public static class DependencyInjection
{
    /// <summary>Registra los servicios de casos de uso de la capa Application (sin infraestructura).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILandlordService, LandlordService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetBlockService, AssetBlockService>();
        services.AddScoped<IAssetGroupService, AssetGroupService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRentalService, RentalService>();
        services.AddScoped<IRentalImportService, RentalImportService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IMeService, MeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IPortalLinkService, PortalLinkService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IPortalPaymentService, PortalPaymentService>();
        services.AddScoped<IContractTemplateService, ContractTemplateService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IRentalRequestService, RentalRequestService>();
        services.AddScoped<IMyRentalService, MyRentalService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<IModuleProvisioner, ModuleProvisioner>();
        services.AddScoped<ICarwashService, CarwashService>();
        services.AddScoped<ICarwashMetricsService, CarwashMetricsService>();

        return services;
    }
}
