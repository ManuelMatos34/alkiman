using Alkiman.Application.AssetBlocks;
using Alkiman.Application.AssetGroups;
using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Carwash;
using Alkiman.Application.Categories;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.ContractTemplates;
using Alkiman.Application.Contracts;
using Alkiman.Application.Customers;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Locations;
using Alkiman.Application.Modules;
using Alkiman.Application.Payments;
using Alkiman.Application.Permissions;
using Alkiman.Application.Portal;
using Alkiman.Application.PortalLinks;
using Alkiman.Application.RentalRequests;
using Alkiman.Application.Rentals;
using Alkiman.Application.Reminders;
using Alkiman.Application.Reports;
using Alkiman.Application.Roles;
using Alkiman.Application.Users;
using Alkiman.Application.Payments.Gateways;
using Alkiman.Infrastructure.BackgroundJobs;
using Alkiman.Infrastructure.Email;
using Alkiman.Infrastructure.Payments;
using Alkiman.Infrastructure.Pdf;
using Alkiman.Infrastructure.Persistence;
using Alkiman.Infrastructure.Persistence.TypeHandlers;
using Alkiman.Infrastructure.Repositories;
using Alkiman.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Alkiman.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra el acceso a datos (Dapper + SQL Server), seguridad, gateways de pago sandbox y sus repositorios.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DapperTypeHandlerRegistration.RegisterAll();

        // QuestPDF (generación de los PDF de contrato) requiere elegir explícitamente una
        // licencia: Community es gratis para individuos/empresas pequeñas (ver questpdf.com/license).
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<ILandlordRepository, LandlordRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetBlockRepository, AssetBlockRepository>();
        services.AddScoped<IAssetGroupRepository, AssetGroupRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IPortalLinkRepository, PortalLinkRepository>();
        services.AddScoped<IPortalRepository, PortalRepository>();
        services.AddScoped<IContractTemplateRepository, ContractTemplateRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IRentalRequestRepository, RentalRequestRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ICarwashServiceRepository, CarwashServiceRepository>();
        services.AddScoped<ICarwashExtraRepository, CarwashExtraRepository>();
        services.AddScoped<ICarwashSettingsRepository, CarwashSettingsRepository>();
        services.AddScoped<ICarwashPortalLinkRepository, CarwashPortalLinkRepository>();
        services.AddScoped<ICarwashTicketRepository, CarwashTicketRepository>();
        services.AddScoped<ICarwashWasherRepository, CarwashWasherRepository>();
        services.AddSingleton<IContractPdfRenderer, QuestPdfContractRenderer>();

        // Job en background: genera recordatorios automáticos 2 días antes del vencimiento
        // de cada renta activa, para todos los landlords (ver RentalDueReminderBackgroundService).
        services.AddHostedService<RentalDueReminderBackgroundService>();

        // Envío real de correo vía Resend (ver Email:ResendApiKey/FromAddress/FromName en
        // configuración). Si no hay API key configurada, ResendEmailSender devuelve un
        // EmailSendResult de fallo explícito (mismo comportamiento que tenía NoOpEmailSender),
        // así que no hace falta un fallback condicional acá.
        services.AddHttpClient<IEmailSender, ResendEmailSender>();

        // Gateway de pago sandbox/test (Stripe): ver Alkiman.Application.Payments.Gateways.
        services.AddScoped<IStripeGateway, StripeGateway>();

        return services;
    }
}
