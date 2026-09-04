using Alkiman.Application.Reminders;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alkiman.Infrastructure.BackgroundJobs;

/// <summary>
/// Primer background job de la app: una vez al día, revisa las rentas activas de TODOS los
/// negocios (no hay contexto HTTP acá, así que no se puede usar ICurrentLandlordService) cuyo
/// EndDate cae dentro de 2 días, y crea un Reminder automático (uno por renta, sin duplicar en
/// corridas subsiguientes) para que el negocio no se olvide de cobrar/gestionar el vencimiento.
/// </summary>
public class RentalDueReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private const int DaysBeforeDue = 2;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RentalDueReminderBackgroundService> _logger;

    public RentalDueReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<RentalDueReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Nunca debe tirar abajo el proceso: se reintenta en el próximo ciclo.
                _logger.LogError(ex, "Fallo al generar recordatorios automáticos de vencimiento de rentas.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del host.
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var rentalRepository = scope.ServiceProvider.GetRequiredService<IRentalRepository>();
        var reminderRepository = scope.ServiceProvider.GetRequiredService<IReminderRepository>();

        var dueDate = DateTime.UtcNow.Date.AddDays(DaysBeforeDue);
        var dueRentals = await rentalRepository.GetActiveRentalsDueOnAsync(dueDate, cancellationToken);

        foreach (var due in dueRentals)
        {
            if (await reminderRepository.ExistsAutoReminderForRentalAsync(due.RentalId, cancellationToken))
                continue;

            var reminder = new Reminder
            {
                LandlordId = due.LandlordId,
                CustomerId = due.CustomerId,
                RentalId = due.RentalId,
                Title = $"Vencimiento próximo: {due.AssetName}",
                Message = $"La renta de \"{due.AssetName}\" vence el {due.EndDate:dd/MM/yyyy} (total {due.TotalPrice:N2}). Quedan {DaysBeforeDue} días.",
                RemindAt = DateTime.UtcNow,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = AutoReminderDefaults.RentalDueCreatedBy,
            };
            await reminderRepository.CreateAsync(reminder, cancellationToken);
        }

        _logger.LogInformation("Recordatorios automáticos de vencimiento: {Count} generados.", dueRentals.Count);
    }
}
