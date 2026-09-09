using System.Text;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Payments.Gateways;
using Alkiman.Application.Portal;
using Alkiman.Application.Users;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Carwash;

/// <summary>
/// Administración de la cola de Carwash: catálogo de servicios, links de
/// portal público y el ciclo de vida de los tickets (alta presencial, avance
/// de estado, llamado/confirmación de llegada, cancelación/expiración), más
/// los flujos públicos (auto-registro y estado del turno por AccessToken).
/// Todos los métodos autenticados se resuelven contra el negocio actual
/// (<see cref="ICurrentLandlordService"/>); los públicos SOLO se resuelven
/// por Slug/AccessToken, nunca por un LandlordId enviado por el caller.
/// </summary>
public class CarwashService : ICarwashService
{
    /// <summary>CreatedBy usado para los registros que origina el flujo público (sin usuario autenticado), mismo criterio que PortalService.PortalCreatedBy.</summary>
    private const string PortalCreatedBy = "carwash-portal";
    private const int DefaultArrivalDeadlineMinutes = 15;

    /// <summary>Porcentaje sugerido de propina cuando el negocio todavía no eligió uno. Espejo del DEFAULT de CWS_Settings en el script 20.</summary>
    private const decimal DefaultTipSuggestedPercent = 10m;

    /// <summary>
    /// Transiciones válidas de AdvanceStatusAsync: estado destino -> estados
    /// actuales que lo admiten. Es una lista y no un único origen porque el
    /// encerado es OPCIONAL: un auto sin encerado va Drying -> Ready directo,
    /// y uno con encerado pasa por Waxing.
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedCurrentStatusesByTarget = new()
    {
        [CarwashTicketStatus.InProgress] = [CarwashTicketStatus.Waiting],
        [CarwashTicketStatus.Drying] = [CarwashTicketStatus.InProgress],
        [CarwashTicketStatus.Waxing] = [CarwashTicketStatus.Drying],
        [CarwashTicketStatus.Ready] = [CarwashTicketStatus.Drying, CarwashTicketStatus.Waxing],
        [CarwashTicketStatus.Delivered] = [CarwashTicketStatus.Ready],
    };

    private readonly ICarwashServiceRepository _serviceRepository;
    private readonly ICarwashExtraRepository _extraRepository;
    private readonly ICarwashSettingsRepository _settingsRepository;
    private readonly ICarwashPortalLinkRepository _portalLinkRepository;
    private readonly ICarwashTicketRepository _ticketRepository;
    private readonly ICarwashWasherRepository _washerRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILandlordRepository _landlordRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;
    private readonly IStripeGateway _stripeGateway;

    public CarwashService(
        ICarwashServiceRepository serviceRepository,
        ICarwashExtraRepository extraRepository,
        ICarwashSettingsRepository settingsRepository,
        ICarwashPortalLinkRepository portalLinkRepository,
        ICarwashTicketRepository ticketRepository,
        ICarwashWasherRepository washerRepository,
        ICustomerRepository customerRepository,
        ILandlordRepository landlordRepository,
        IUserRepository userRepository,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog,
        IEmailSender emailSender,
        IStripeGateway stripeGateway)
    {
        _serviceRepository = serviceRepository;
        _extraRepository = extraRepository;
        _settingsRepository = settingsRepository;
        _portalLinkRepository = portalLinkRepository;
        _ticketRepository = ticketRepository;
        _washerRepository = washerRepository;
        _customerRepository = customerRepository;
        _landlordRepository = landlordRepository;
        _userRepository = userRepository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
        _emailSender = emailSender;
        _stripeGateway = stripeGateway;
    }

    // ============================================================
    // Configuración del módulo
    // ============================================================

    public async Task<CarwashSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var settings = await _settingsRepository.GetByLandlordAsync(landlordId, cancellationToken);
        return new CarwashSettingsResponse(
            settings?.OperationMode,
            settings?.TipMode ?? CarwashTipMode.Optional,
            settings?.TipSuggestedPercent ?? DefaultTipSuggestedPercent);
    }

    public async Task<CarwashSettingsResponse> SaveSettingsAsync(SaveCarwashSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OperationMode is not (CarwashOperationMode.Empresa or CarwashOperationMode.Solitario))
            throw new AppValidationException($"Modo de operación inválido: '{request.OperationMode}'.");

        // El diálogo de configuración inicial sólo pregunta el modo de operación,
        // así que TipMode llega null desde ahí: se cae al default en vez de
        // obligar a decidir sobre propinas antes de haber lavado un solo auto.
        var tipMode = request.TipMode ?? CarwashTipMode.Optional;
        if (!CarwashTipMode.IsValid(tipMode))
            throw new AppValidationException($"Modo de propina inválido: '{request.TipMode}'.");

        var tipPercent = request.TipSuggestedPercent ?? DefaultTipSuggestedPercent;
        if (tipPercent is < 0m or > 100m)
            throw new AppValidationException("El porcentaje sugerido de propina debe estar entre 0 y 100.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var settings = new CarwashSettings
        {
            LandlordId = landlordId,
            OperationMode = request.OperationMode,
            TipMode = tipMode,
            TipSuggestedPercent = tipPercent,
            CreatedAt = now,
            CreatedBy = _currentLandlord.UserId,
            UpdatedAt = now,
            UpdatedBy = _currentLandlord.UserId
        };

        await _settingsRepository.UpsertAsync(settings, cancellationToken);

        // En modo Empresa no se siembra nada. Antes se creaba acá el rol de sistema
        // "Lavador"; se quitó porque le aparecía al negocio en el mantenimiento de
        // roles sin haberlo pedido y, por ser IsSystem, tampoco lo podía borrar. Quien
        // quiera un usuario que trabaje la cola arma el rol a mano con carwash.view y
        // carwash.work. Ojo: quien lava NO necesita cuenta —es un CarwashWasher, una
        // ficha del módulo—, así que el modo Empresa funciona igual sin ningún rol.
        if (request.OperationMode != CarwashOperationMode.Empresa)
        {
            // En modo Solitario el que lava es el propio dueño, así que se le deja
            // cargada su ficha: sin ella el auto-asignado de AdvanceStatusAsync no
            // tendría a quién apuntar y los turnos quedarían sin nombre en el historial.
            await EnsureSoloWasherAsync(landlordId, cancellationToken);
        }

        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Settings", landlordId.ToString(), null, settings, cancellationToken);
        return new CarwashSettingsResponse(settings.OperationMode, settings.TipMode, settings.TipSuggestedPercent);
    }

    // ============================================================
    // Catálogo de servicios
    // ============================================================

    public async Task<IReadOnlyList<CarwashServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var services = await _serviceRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return services.Select(ToServiceResponse).ToList();
    }

    public async Task<CarwashServiceResponse> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateServiceRequest(request.Name, request.Price, request.EstimatedMinutes);

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var service = new CarwashServiceItem
        {
            LandlordId = landlordId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            EstimatedMinutes = request.EstimatedMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        service.Id = await _serviceRepository.CreateAsync(service, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CWS_Services", service.Id.ToString(), null, service, cancellationToken);
        return ToServiceResponse(service);
    }

    public async Task<CarwashServiceResponse> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateServiceRequest(request.Name, request.Price, request.EstimatedMinutes);

        var service = await GetOwnedServiceOrThrowAsync(id, cancellationToken);
        service.Name = request.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        service.Price = request.Price;
        service.EstimatedMinutes = request.EstimatedMinutes;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;
        service.UpdatedBy = _currentLandlord.UserId;

        await _serviceRepository.UpdateAsync(service, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Services", service.Id.ToString(), null, service, cancellationToken);
        return ToServiceResponse(service);
    }

    public async Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await GetOwnedServiceOrThrowAsync(id, cancellationToken);

        // Un servicio que ya se usó en algún ticket no se puede borrar (violaría la FK de
        // CWS_Tickets); desactivarlo logra el mismo efecto de dejar de ofrecerlo sin perder
        // el historial (mismo criterio que PortalLinkService.DeleteAsync con PRT_PortalRentals).
        if (await _serviceRepository.HasTicketsAsync(id, cancellationToken))
            throw new AppValidationException("No se puede eliminar un servicio que ya fue usado en tickets. Desactívalo en su lugar.");

        await _serviceRepository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CWS_Services", service.Id.ToString(), service, null, cancellationToken);
    }

    private static void ValidateServiceRequest(string name, decimal price, int estimatedMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppValidationException("El nombre del servicio es obligatorio.");
        if (price < 0)
            throw new AppValidationException("El precio no puede ser negativo.");
        if (estimatedMinutes <= 0)
            throw new AppValidationException("El tiempo estimado debe ser mayor a cero.");
    }

    private async Task<CarwashServiceItem> GetOwnedServiceOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashServiceItem), id);

        if (service.LandlordId != landlordId)
            throw new ForbiddenException("El servicio no pertenece al negocio autenticado.");

        return service;
    }

    private static CarwashServiceResponse ToServiceResponse(CarwashServiceItem service) => new(
        service.Id, service.Name, service.Description, service.Price, service.EstimatedMinutes, service.IsActive);

    // ============================================================
    // Catálogo de extras
    // ============================================================

    public async Task<IReadOnlyList<CarwashExtraResponse>> GetExtrasAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var extras = await _extraRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return extras.Select(ToExtraResponse).ToList();
    }

    public async Task<CarwashExtraResponse> CreateExtraAsync(CreateExtraRequest request, CancellationToken cancellationToken = default)
    {
        ValidateExtraRequest(request.Name, request.Price, request.EstimatedMinutes);

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var extra = new CarwashServiceExtra
        {
            LandlordId = landlordId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            EstimatedMinutes = request.EstimatedMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        extra.Id = await _extraRepository.CreateAsync(extra, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CWS_ServiceExtras", extra.Id.ToString(), null, extra, cancellationToken);
        return ToExtraResponse(extra);
    }

    public async Task<CarwashExtraResponse> UpdateExtraAsync(int id, UpdateExtraRequest request, CancellationToken cancellationToken = default)
    {
        ValidateExtraRequest(request.Name, request.Price, request.EstimatedMinutes);

        var extra = await GetOwnedExtraOrThrowAsync(id, cancellationToken);
        extra.Name = request.Name.Trim();
        extra.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        extra.Price = request.Price;
        extra.EstimatedMinutes = request.EstimatedMinutes;
        extra.IsActive = request.IsActive;
        extra.UpdatedAt = DateTime.UtcNow;
        extra.UpdatedBy = _currentLandlord.UserId;

        await _extraRepository.UpdateAsync(extra, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_ServiceExtras", extra.Id.ToString(), null, extra, cancellationToken);
        return ToExtraResponse(extra);
    }

    public async Task DeleteExtraAsync(int id, CancellationToken cancellationToken = default)
    {
        var extra = await GetOwnedExtraOrThrowAsync(id, cancellationToken);

        // Mismo criterio que DeleteServiceAsync: si ya se usó, desactivarlo en vez de
        // borrarlo (la FK de CWS_TicketExtras lo impide y perderíamos el historial).
        if (await _ticketRepository.HasTicketsWithExtraAsync(id, cancellationToken))
            throw new AppValidationException("No se puede eliminar un extra que ya fue usado en tickets. Desactivalo en su lugar.");

        await _extraRepository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CWS_ServiceExtras", extra.Id.ToString(), extra, null, cancellationToken);
    }

    private static void ValidateExtraRequest(string name, decimal price, int estimatedMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppValidationException("El nombre del extra es obligatorio.");
        if (price < 0)
            throw new AppValidationException("El precio no puede ser negativo.");
        // A diferencia del servicio base, un extra puede no sumar tiempo (0 es válido).
        if (estimatedMinutes < 0)
            throw new AppValidationException("El tiempo estimado no puede ser negativo.");
    }

    private async Task<CarwashServiceExtra> GetOwnedExtraOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var extra = await _extraRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashServiceExtra), id);

        if (extra.LandlordId != landlordId)
            throw new ForbiddenException("El extra no pertenece al negocio autenticado.");

        return extra;
    }

    private static CarwashExtraResponse ToExtraResponse(CarwashServiceExtra extra) => new(
        extra.Id, extra.Name, extra.Description, extra.Price, extra.EstimatedMinutes, extra.IsActive);

    /// <summary>
    /// Valida que los extras pedidos existan, pertenezcan al negocio y estén
    /// activos, y devuelve las filas de detalle con el nombre y el precio ya
    /// congelados. Compartido entre el alta presencial y la del portal público.
    /// </summary>
    private async Task<List<CarwashTicketExtra>> BuildTicketExtrasAsync(
        Guid landlordId, Guid ticketId, IReadOnlyList<int>? extraIds, CancellationToken cancellationToken)
    {
        if (extraIds is null || extraIds.Count == 0)
            return [];

        var catalog = await _extraRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        var rows = new List<CarwashTicketExtra>();

        foreach (var extraId in extraIds.Distinct())
        {
            var extra = catalog.FirstOrDefault(e => e.Id == extraId && e.IsActive)
                ?? throw new AppValidationException("Uno de los extras seleccionados no está disponible.");

            rows.Add(new CarwashTicketExtra
            {
                TicketId = ticketId,
                ExtraId = extra.Id,
                Name = extra.Name,
                Price = extra.Price
            });
        }

        return rows;
    }

    // ============================================================
    // Links de portal
    // ============================================================

    public async Task<IReadOnlyList<CarwashPortalLinkResponse>> GetPortalLinksAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var links = await _portalLinkRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return links.Select(ToPortalLinkResponse).ToList();
    }

    public async Task<CarwashPortalLinkResponse> CreatePortalLinkAsync(CreateCarwashPortalLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 150)
            throw new AppValidationException("El título del link es inválido.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var title = request.Title.Trim();
        var link = new CarwashPortalLink
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            Title = title,
            Slug = await GenerateUniqueSlugAsync(title, cancellationToken),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _portalLinkRepository.CreateAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CWS_PortalLinks", link.Id.ToString(), null, link, cancellationToken);
        return ToPortalLinkResponse(link);
    }

    public async Task<CarwashPortalLinkResponse> SetPortalLinkActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedPortalLinkOrThrowAsync(id, cancellationToken);
        link.IsActive = isActive;
        link.UpdatedAt = DateTime.UtcNow;
        link.UpdatedBy = _currentLandlord.UserId;

        await _portalLinkRepository.UpdateAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_PortalLinks", link.Id.ToString(), null, link, cancellationToken);
        return ToPortalLinkResponse(link);
    }

    public async Task DeletePortalLinkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedPortalLinkOrThrowAsync(id, cancellationToken);
        if (await _portalLinkRepository.HasTicketsAsync(link.Id, cancellationToken))
            throw new AppValidationException("No se puede eliminar este link porque ya tiene turnos asociados. Desactívalo en su lugar.");
        await _portalLinkRepository.DeleteAsync(link.Id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CWS_PortalLinks", link.Id.ToString(), link, null, cancellationToken);
    }

    private async Task<CarwashPortalLink> GetOwnedPortalLinkOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var link = await _portalLinkRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashPortalLink), id);

        if (link.LandlordId != landlordId)
            throw new ForbiddenException("El link no pertenece al negocio autenticado.");

        return link;
    }

    private static CarwashPortalLinkResponse ToPortalLinkResponse(CarwashPortalLink link) => new(
        link.Id, link.Title, link.Slug, link.IsActive, link.CreatedAt);

    /// <summary>Genera un slug legible a partir del título; si ya existe, le agrega un sufijo aleatorio corto (mismo helper que PortalLinkService.GenerateUniqueSlugAsync).</summary>
    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var attempts = 0;

        while (await _portalLinkRepository.SlugExistsAsync(slug, cancellationToken))
        {
            attempts++;
            var suffix = Guid.NewGuid().ToString("N")[..6];
            slug = $"{baseSlug}-{suffix}";

            if (attempts > 10)
                throw new AppValidationException("No se pudo generar un identificador único para el link. Intentá de nuevo.");
        }

        return slug;
    }

    private static string Slugify(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var builder = new StringBuilder();
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "lavado";

        return slug.Length > 30 ? slug[..30].Trim('-') : slug;
    }

    // ============================================================
    // Cola (autenticado)
    // ============================================================

    public async Task<IReadOnlyList<CarwashTicketResponse>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var tickets = await _ticketRepository.GetActiveByLandlordAsync(landlordId, cancellationToken);
        if (tickets.Count == 0)
            return [];

        // Los extras de todo el tablero se traen de una sola vez y se reparten por
        // ticket, en vez de una consulta por tarjeta.
        var extrasByTicket = (await _ticketRepository.GetExtrasByTicketIdsAsync([.. tickets.Select(t => t.Id)], cancellationToken))
            .GroupBy(e => e.TicketId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CarwashTicketExtra>)[.. g]);

        var responses = new List<CarwashTicketResponse>(tickets.Count);
        foreach (var ticket in tickets)
        {
            var extras = extrasByTicket.GetValueOrDefault(ticket.Id, []);
            responses.Add(await ToTicketResponseAsync(ticket, extras, cancellationToken));
        }
        return responses;
    }

    // ============================================================
    // Directorio de lavadores
    //
    // Un lavador es una entidad del módulo, NO un usuario del sistema: ver
    // CarwashWasher. Antes esta lista se derivaba de "usuarios cuyo rol tiene
    // carwash.work", que obligaba a crearle cuenta a cada persona que lava y
    // metía al Administrador en el desplegable.
    // ============================================================

    public async Task<IReadOnlyList<CarwashWasherResponse>> GetWashersAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var washers = await _washerRepository.GetAllByLandlordAsync(landlordId, cancellationToken);

        var responses = new List<CarwashWasherResponse>(washers.Count);
        foreach (var washer in washers)
            responses.Add(await ToWasherResponseAsync(washer, cancellationToken));

        return responses;
    }

    public async Task<CarwashWasherResponse> CreateWasherAsync(CreateWasherRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var fullName = ValidateWasherName(request.FullName);

        var washer = new CarwashWasher
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = fullName,
            Phone = Normalize(request.Phone),
            Email = Normalize(request.Email),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _washerRepository.CreateAsync(washer, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CWS_Washers", washer.Id.ToString(), null, washer, cancellationToken);
        return await ToWasherResponseAsync(washer, cancellationToken);
    }

    public async Task<CarwashWasherResponse> UpdateWasherAsync(Guid id, UpdateWasherRequest request, CancellationToken cancellationToken = default)
    {
        var washer = await GetOwnedWasherOrThrowAsync(id, cancellationToken);
        var fullName = ValidateWasherName(request.FullName);

        washer.FullName = fullName;
        washer.Phone = Normalize(request.Phone);
        washer.Email = Normalize(request.Email);
        washer.IsActive = request.IsActive;
        washer.UpdatedAt = DateTime.UtcNow;
        washer.UpdatedBy = _currentLandlord.UserId;

        await _washerRepository.UpdateAsync(washer, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Washers", washer.Id.ToString(), null, washer, cancellationToken);
        return await ToWasherResponseAsync(washer, cancellationToken);
    }

    public async Task DeleteWasherAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var washer = await GetOwnedWasherOrThrowAsync(id, cancellationToken);

        // Borrarlo rompería la FK de los tickets y, sobre todo, dejaría lavados
        // viejos sin nombre. Desactivarlo lo saca de las asignaciones nuevas y
        // conserva el historial.
        if (await _washerRepository.HasTicketsAsync(washer.Id, cancellationToken))
            throw new AppValidationException("Este lavador ya tiene turnos registrados. Desactivalo en vez de eliminarlo para no perder el historial.");

        await _washerRepository.DeleteAsync(washer.Id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CWS_Washers", washer.Id.ToString(), washer, null, cancellationToken);
    }

    private async Task<CarwashWasher> GetOwnedWasherOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var washer = await _washerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashWasher), id);

        if (washer.LandlordId != landlordId)
            throw new ForbiddenException("El lavador no pertenece al negocio autenticado.");

        return washer;
    }

    private static string ValidateWasherName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new AppValidationException("El nombre del lavador es obligatorio.");
        return fullName.Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Deja al negocio con su único lavador en modo Solitario.
    ///
    /// Idempotente: se llama cada vez que se guarda el modo. Si ya hay algún lavador
    /// cargado no toca nada —el usuario puede haberle puesto el nombre que quiso— y
    /// sólo crea la ficha inicial cuando el plantel está vacío. El nombre sale de la
    /// cuenta que está operando por comodidad, pero es un nombre y nada más: la ficha
    /// no queda atada a esa cuenta de ninguna forma.
    /// </summary>
    private async Task EnsureSoloWasherAsync(Guid landlordId, CancellationToken cancellationToken)
    {
        var washers = await _washerRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        if (washers.Count > 0)
            return;

        var fullName = "Yo";
        if (Guid.TryParse(_currentLandlord.UserId, out var currentUserId))
        {
            var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (user is not null && user.LandlordId == landlordId && !string.IsNullOrWhiteSpace(user.FullName))
                fullName = user.FullName;
        }

        var washer = new CarwashWasher
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _washerRepository.CreateAsync(washer, cancellationToken);
    }

    private async Task<CarwashWasherResponse> ToWasherResponseAsync(CarwashWasher washer, CancellationToken cancellationToken)
    {
        var canDelete = !await _washerRepository.HasTicketsAsync(washer.Id, cancellationToken);

        return new CarwashWasherResponse(
            washer.Id, washer.FullName, washer.Phone, washer.Email, washer.IsActive, canDelete);
    }

    /// <summary>
    /// El lavador al que le toca el trabajo en modo Solitario: el único activo del
    /// negocio. Si hay varios devuelve null y la asignación queda manual — con más
    /// de una persona el negocio ya no es Solitario, y adivinar a quién atribuirle
    /// el vehículo ensuciaría el ranking y las propinas.
    /// </summary>
    private async Task<CarwashWasher?> GetSoloWasherAsync(Guid landlordId, CancellationToken cancellationToken)
        => await _washerRepository.GetSingleActiveAsync(landlordId, cancellationToken);

    public async Task<CarwashTicketResponse> AssignWasherAsync(Guid ticketId, AssignWasherRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (ticket.Status is CarwashTicketStatus.Delivered or CarwashTicketStatus.Cancelled or CarwashTicketStatus.Expired)
            throw new AppValidationException("Este turno ya fue finalizado y no se puede reasignar.");

        if (request.WasherId is { } washerId)
        {
            var washer = await _washerRepository.GetByIdAsync(washerId, cancellationToken)
                ?? throw new NotFoundException(nameof(CarwashWasher), washerId);

            if (washer.LandlordId != ticket.LandlordId || !washer.IsActive)
                throw new AppValidationException("El lavador seleccionado no está disponible.");
        }

        ticket.AssignedToWasherId = request.WasherId;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    public async Task<CarwashTicketResponse> RegisterTicketAsync(RegisterTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new AppValidationException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.VehiclePlate))
            throw new AppValidationException("La placa del vehículo es obligatoria.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashServiceItem), request.ServiceId);
        if (service.LandlordId != landlordId)
            throw new ForbiddenException("El servicio no pertenece al negocio autenticado.");

        var now = DateTime.UtcNow;
        var customer = await CreateTicketCustomerAsync(
            landlordId, request.CustomerName, request.CustomerPhone, request.CustomerEmail, _currentLandlord.UserId, now, cancellationToken);
        var queueNumber = await _ticketRepository.GetNextQueueNumberAsync(landlordId, cancellationToken);

        var ticket = new CarwashTicket
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            CustomerId = customer.Id,
            CustomerName = request.CustomerName.Trim(),
            ServiceId = service.Id,
            QueueNumber = queueNumber,
            VehiclePlate = NormalizePlate(request.VehiclePlate),
            VehicleBrand = Trimmed(request.VehicleBrand),
            VehicleModel = Trimmed(request.VehicleModel),
            VehicleYear = ValidateVehicleYear(request.VehicleYear),
            VehicleColor = Trimmed(request.VehicleColor),
            ServicePrice = service.Price,
            Status = CarwashTicketStatus.Waiting,
            Source = CarwashTicketSource.Presencial,
            AccessToken = Guid.NewGuid(),
            CreatedAt = now,
            CreatedBy = _currentLandlord.UserId
        };

        var extras = await BuildTicketExtrasAsync(landlordId, ticket.Id, request.ExtraIds, cancellationToken);

        await _ticketRepository.CreateAsync(ticket, cancellationToken);
        await _ticketRepository.AddExtrasAsync(extras, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, extras, cancellationToken);
    }

    public async Task<CarwashTicketResponse> AdvanceStatusAsync(Guid ticketId, AdvanceStatusRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (!AllowedCurrentStatusesByTarget.TryGetValue(request.Status, out var allowedCurrentStatuses))
            throw new AppValidationException($"Estado de destino inválido: '{request.Status}'.");

        if (!allowedCurrentStatuses.Contains(ticket.Status))
            throw new AppValidationException($"No se puede pasar de '{ticket.Status}' a '{request.Status}'.");

        var now = DateTime.UtcNow;
        ticket.Status = request.Status;
        switch (request.Status)
        {
            case CarwashTicketStatus.InProgress:
                ticket.StartedAt = now;
                if (ticket.AssignedToWasherId is null)
                {
                    // En modo Solitario el único lavador activo se auto-asigna.
                    if (await IsSoloModeAsync(ticket.LandlordId, cancellationToken))
                    {
                        var soloWasher = await GetSoloWasherAsync(ticket.LandlordId, cancellationToken);
                        if (soloWasher is not null)
                            ticket.AssignedToWasherId = soloWasher.Id;
                    }
                    else
                    {
                        // Modo Empresa: el encargado debe asignar un lavador antes de iniciar.
                        throw new AppValidationException("Debes asignar un lavador antes de iniciar el lavado.");
                    }
                }
                break;
            case CarwashTicketStatus.Ready:
                ticket.ReadyAt = now;
                break;
            case CarwashTicketStatus.Delivered:
                ticket.DeliveredAt = now;
                ApplyTip(ticket, request.TipAmount);
                break;
        }
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        await TryNotifyCustomerAsync(ticket, cancellationToken);

        if (ticket.Status == CarwashTicketStatus.Delivered)
        {
            await TrySendInvoiceAsync(ticket, cancellationToken);
            await TrySendWasherTipNotificationAsync(ticket, cancellationToken);
        }

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    /// <summary>Retrocede el estado al paso anterior. Útil cuando el encargado avanzó por error.</summary>
    public async Task<CarwashTicketResponse> GoBackStatusAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        var previousStatus = ticket.Status switch
        {
            CarwashTicketStatus.InProgress => CarwashTicketStatus.Waiting,
            CarwashTicketStatus.Drying     => CarwashTicketStatus.InProgress,
            CarwashTicketStatus.Waxing     => CarwashTicketStatus.Drying,
            CarwashTicketStatus.Ready      => CarwashTicketStatus.Drying,
            _ => throw new AppValidationException($"No se puede retroceder desde el estado '{ticket.Status}'.")
        };

        var now = DateTime.UtcNow;
        ticket.Status = previousStatus;

        // Limpiar timestamps del estado que se deshace
        switch (previousStatus)
        {
            case CarwashTicketStatus.Waiting:
                ticket.StartedAt = null;
                break;
            case CarwashTicketStatus.InProgress:
            case CarwashTicketStatus.Drying:
                ticket.ReadyAt = null;
                break;
        }

        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    public async Task<CarwashTicketResponse> CancelTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (ticket.Status is CarwashTicketStatus.Delivered or CarwashTicketStatus.Cancelled or CarwashTicketStatus.Expired)
            throw new AppValidationException("Este turno ya fue finalizado y no se puede cancelar.");

        var now = DateTime.UtcNow;
        ticket.Status = CarwashTicketStatus.Cancelled;
        ticket.CancelledAt = now;
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        await TryNotifyCustomerAsync(ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    public async Task<CarwashTicketResponse> MarkExpiredAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (ticket.Status != CarwashTicketStatus.ArrivalPending)
            throw new AppValidationException("Solo se puede marcar como expirado un turno que está esperando confirmación de llegada.");

        var now = DateTime.UtcNow;
        ticket.Status = CarwashTicketStatus.Expired;
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    public async Task<CarwashTicketResponse> CallArrivalAsync(Guid ticketId, int? deadlineMinutes, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (ticket.Source != CarwashTicketSource.Portal)
            throw new AppValidationException("Solo los turnos registrados desde el portal público requieren confirmación de llegada.");
        if (ticket.Status != CarwashTicketStatus.Waiting)
            throw new AppValidationException("Solo se puede llamar un turno que está en espera.");

        var minutes = deadlineMinutes is > 0 ? deadlineMinutes.Value : DefaultArrivalDeadlineMinutes;
        var now = DateTime.UtcNow;
        ticket.Status = CarwashTicketStatus.ArrivalPending;
        ticket.ArrivalDeadline = now.AddMinutes(minutes);
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        await TryNotifyCustomerAsync(ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    public async Task<CarwashTicketResponse> ConfirmArrivalAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await GetOwnedTicketOrThrowAsync(ticketId, cancellationToken);

        if (ticket.Status != CarwashTicketStatus.ArrivalPending)
            throw new AppValidationException("Este turno no está esperando confirmación de llegada.");

        var now = DateTime.UtcNow;
        ticket.Status = CarwashTicketStatus.Waiting;
        ticket.ArrivedAt = now;
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        return await ToTicketResponseAsync(ticket, cancellationToken);
    }

    /// <summary>
    /// Deja constancia de la propina al entregar el vehículo. NO cobra nada: el
    /// módulo no procesa pagos, el monto es lo que el mostrador dice haber recibido
    /// en efectivo.
    ///
    /// El lavador se congela acá y no se lee después de AssignedToWasherId a
    /// propósito (ver <see cref="CarwashTicket.TipWasherId"/>): reasignar un ticket
    /// ya entregado no debe mover plata de una persona a otra.
    ///
    /// Sin propina (null o 0) las dos columnas quedan en null. Un 0 explícito y un
    /// null significan lo mismo para el negocio, y guardar sólo una de las dos
    /// formas evita que el ranking tenga que decidir cuál es cuál.
    ///
    /// Los turnos del portal son la excepción: su propina ya se cobró por la
    /// pasarela al reservar y el monto no se toca acá. El tablero ni siquiera
    /// pregunta en esos casos, así que <paramref name="tipAmount"/> llegaría en
    /// null y borraría plata realmente cobrada. Lo único que sí falta congelar es
    /// a quién se le atribuye, porque al reservar todavía no había lavador.
    /// </summary>
    private static void ApplyTip(CarwashTicket ticket, decimal? tipAmount)
    {
        if (ticket.TipPrepaid)
        {
            if (ticket.TipAmount is > 0m)
                ticket.TipWasherId = ticket.AssignedToWasherId;
            return;
        }

        if (tipAmount is null or <= 0m)
        {
            if (tipAmount < 0m)
                throw new AppValidationException("La propina no puede ser negativa.");

            ticket.TipAmount = null;
            ticket.TipWasherId = null;
            return;
        }

        ticket.TipAmount = decimal.Round(tipAmount.Value, 2, MidpointRounding.AwayFromZero);

        // Si nadie quedó asignado al lavado, la propina igual se registra: entró
        // plata al negocio. Simplemente no se le atribuye a nadie, en vez de
        // inventarle un dueño para que la tabla quede prolija.
        ticket.TipWasherId = ticket.AssignedToWasherId;

        // A propósito NO se valida contra el TipMode del negocio. TipMode decide
        // qué PREGUNTA el mostrador, no qué acepta la base: rechazar la entrega de
        // un auto ya lavado porque el front quedó desactualizado deja al cliente
        // esperando en la caja por un problema de configuración.
    }

    private async Task<CarwashTicket> GetOwnedTicketOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var ticket = await _ticketRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashTicket), id);

        if (ticket.LandlordId != landlordId)
            throw new ForbiddenException("El turno no pertenece al negocio autenticado.");

        return ticket;
    }

    /// <summary>Overload para un ticket suelto: resuelve sus extras por su cuenta.</summary>
    private async Task<CarwashTicketResponse> ToTicketResponseAsync(CarwashTicket ticket, CancellationToken cancellationToken)
    {
        var extras = await _ticketRepository.GetExtrasByTicketIdsAsync([ticket.Id], cancellationToken);
        return await ToTicketResponseAsync(ticket, extras, cancellationToken);
    }

    private async Task<CarwashTicketResponse> ToTicketResponseAsync(
        CarwashTicket ticket, IReadOnlyList<CarwashTicketExtra> extras, CancellationToken cancellationToken)
    {
        // CustomerName es el snapshot guardado al registrar el ticket.
        // Se cae al lookup en CRM_Customers solo para tickets históricos que
        // migraron antes de que existiera la columna (valor NULL).
        var customerName = ticket.CustomerName;
        if (string.IsNullOrWhiteSpace(customerName))
        {
            var customer = await _customerRepository.GetByIdAsync(ticket.CustomerId, cancellationToken);
            customerName = customer?.FullName;
        }

        var service = await _serviceRepository.GetByIdAsync(ticket.ServiceId, cancellationToken);

        string? assignedToName = null;
        if (ticket.AssignedToWasherId is { } assignedId)
            assignedToName = (await _washerRepository.GetByIdAsync(assignedId, cancellationToken))?.FullName;

        // Casi siempre es el mismo lavador que el asignado; se reusa el nombre en
        // vez de volver a la base. Sólo difieren si el ticket se reasignó después
        // de entregarlo, que es justo el caso que TipWasherId existe para separar.
        string? tipWasherName = ticket.TipWasherId switch
        {
            null => null,
            var id when id == ticket.AssignedToWasherId => assignedToName,
            var id => (await _washerRepository.GetByIdAsync(id.Value, cancellationToken))?.FullName
        };

        return new CarwashTicketResponse(
            ticket.Id, ticket.QueueNumber, ticket.VehiclePlate,
            ticket.VehicleBrand, ticket.VehicleModel, ticket.VehicleYear, ticket.VehicleColor,
            ticket.CustomerId, customerName ?? "—", null,
            ticket.ServiceId, service?.Name ?? "—", ticket.ServicePrice,
            ToExtraResponses(extras), CalculateTotal(ticket.ServicePrice, extras),
            ticket.AssignedToWasherId, assignedToName,
            ticket.Status, ticket.Source,
            ticket.ArrivalDeadline, ticket.ArrivedAt, ticket.StartedAt, ticket.ReadyAt, ticket.DeliveredAt, ticket.CancelledAt,
            ticket.Notes,
            ticket.TipAmount, ticket.TipWasherId, tipWasherName, ticket.TipPrepaid,
            ticket.CreatedAt);
    }

    private static IReadOnlyList<CarwashTicketExtraResponse> ToExtraResponses(IReadOnlyList<CarwashTicketExtra> extras)
        => [.. extras.Select(e => new CarwashTicketExtraResponse(e.ExtraId, e.Name, e.Price))];

    private static decimal CalculateTotal(decimal servicePrice, IReadOnlyList<CarwashTicketExtra> extras)
        => servicePrice + extras.Sum(e => e.Price);

    // ============================================================
    // Público (sin login, resuelto por Slug/AccessToken)
    // ============================================================

    public async Task<PublicCarwashLinkResponse> GetPublicLinkAsync(string slug, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);
        var landlord = await _landlordRepository.GetByIdAsync(link.LandlordId, cancellationToken);
        var services = await _serviceRepository.GetAllByLandlordAsync(link.LandlordId, cancellationToken);
        var activeServices = services.Where(s => s.IsActive).Select(ToServiceResponse).ToList();
        var extras = await _extraRepository.GetAllByLandlordAsync(link.LandlordId, cancellationToken);
        var activeExtras = extras.Where(e => e.IsActive).Select(ToExtraResponse).ToList();
        var settings = await _settingsRepository.GetByLandlordAsync(link.LandlordId, cancellationToken);

        return new PublicCarwashLinkResponse(
            link.Title,
            landlord?.BusinessName ?? "Alkiman",
            landlord?.AppName ?? "Alkiman",
            landlord?.ThemeMode ?? "light",
            landlord?.AccentColor ?? "blue",
            activeServices,
            activeExtras,
            settings?.TipMode ?? CarwashTipMode.Optional,
            settings?.TipSuggestedPercent ?? DefaultTipSuggestedPercent,
            _stripeGateway.IsConfigured);
    }

    public async Task<PublicTicketStatusResponse> JoinQueueBySlugAsync(string slug, PublicJoinQueueRequest request, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new AppValidationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.VehiclePlate))
            throw new AppValidationException("La placa del vehículo es obligatoria.");

        var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashServiceItem), request.ServiceId);
        if (service.LandlordId != link.LandlordId || !service.IsActive)
            throw new AppValidationException("El servicio seleccionado no está disponible.");

        var now = DateTime.UtcNow;
        var customer = await CreateTicketCustomerAsync(
            link.LandlordId, request.CustomerName, request.CustomerPhone, request.CustomerEmail, PortalCreatedBy, now, cancellationToken);
        var queueNumber = await _ticketRepository.GetNextQueueNumberAsync(link.LandlordId, cancellationToken);

        var ticket = new CarwashTicket
        {
            Id = Guid.NewGuid(),
            LandlordId = link.LandlordId,
            CustomerId = customer.Id,
            CustomerName = request.CustomerName.Trim(),
            ServiceId = service.Id,
            QueueNumber = queueNumber,
            VehiclePlate = NormalizePlate(request.VehiclePlate),
            VehicleBrand = Trimmed(request.VehicleBrand),
            VehicleModel = Trimmed(request.VehicleModel),
            VehicleYear = ValidateVehicleYear(request.VehicleYear),
            VehicleColor = Trimmed(request.VehicleColor),
            ServicePrice = service.Price,
            // MVP: el ticket del portal nace en Waiting, no en ArrivalPending -- la ventana de
            // llegada recién se abre cuando el Encargado "llama" el turno (CallArrivalAsync),
            // manteniendo el modelo simple (ver plan sección 2.2).
            Status = CarwashTicketStatus.Waiting,
            Source = CarwashTicketSource.Portal,
            AccessToken = Guid.NewGuid(),
            CreatedAt = now,
            CreatedBy = PortalCreatedBy
        };

        var extras = await BuildTicketExtrasAsync(link.LandlordId, ticket.Id, request.ExtraIds, cancellationToken);

        await ApplyPortalPaymentAsync(ticket, service.Price, extras, request, cancellationToken);

        await _ticketRepository.CreateAsync(ticket, cancellationToken);
        await _ticketRepository.AddExtrasAsync(extras, cancellationToken);

        var landlord = await _landlordRepository.GetByIdAsync(link.LandlordId, cancellationToken);
        return ToPublicStatus(ticket, service.Name, extras, landlord);
    }

    /// <summary>
    /// Sella en el turno lo que el cliente pagó online, pero sólo después de
    /// confirmarlo contra Stripe.
    ///
    /// Todo lo que llega en el request viene de un endpoint público: el monto, la
    /// propina y la referencia son afirmaciones del navegador, no hechos. Por eso
    /// el total se recalcula acá con los precios del catálogo y se contrasta
    /// contra lo que el gateway dice que realmente entró. Sin ese contraste,
    /// alguien podría reservar un lavado completo pagando un peso.
    ///
    /// Si no viene provider, el turno es de los que se pagan en el mostrador y
    /// las columnas de pago quedan intactas: eso es lo que pasa cuando el negocio
    /// todavía no tiene Stripe configurado.
    /// </summary>
    private async Task ApplyPortalPaymentAsync(
        CarwashTicket ticket,
        decimal servicePrice,
        IReadOnlyList<CarwashTicketExtra> extras,
        PublicJoinQueueRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentProvider))
        {
            // Sin pasarela no hay propina que valga: aceptar un TipAmount suelto
            // acá anotaría plata que nadie entregó, justo lo que el ranking de
            // lavadores no debe contener. La propina en efectivo se registra al
            // entregar el vehículo, como siempre.
            return;
        }

        if (request.TipAmount < 0m)
            throw new AppValidationException("La propina no puede ser negativa.");

        var tip = request.TipAmount is > 0m
            ? decimal.Round(request.TipAmount.Value, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        var charged = CalculateTotal(servicePrice, extras) + (tip ?? 0m);

        if (string.IsNullOrWhiteSpace(request.PaymentReference))
            throw new AppValidationException("El pago no pudo verificarse. Intenta nuevamente.");

        if (!string.Equals(request.PaymentProvider, "Stripe", StringComparison.Ordinal))
            throw new AppValidationException("Proveedor de pago no soportado.");

        var intent = await _stripeGateway.GetPaymentIntentAsync(request.PaymentReference, cancellationToken);
        var expectedAmountInCents = PortalPaymentService.ToGatewayAmountInCents(charged);
        if (intent is null
            || !string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            || intent.AmountInCents != expectedAmountInCents)
            throw new AppValidationException("El pago no pudo verificarse. Intenta nuevamente.");

        ticket.TipAmount = tip;
        // TipPrepaid se marca aunque la propina sea null: lo que la bandera
        // significa es "por esta vía ya se preguntó y se cobró", y un cliente que
        // eligió no dejar propina tampoco debe volver a ser consultado en la caja.
        ticket.TipPrepaid = true;
        ticket.PaymentProvider = request.PaymentProvider;
        ticket.PaymentReference = request.PaymentReference;
        ticket.PaidAmount = charged;

        // TipWasherId queda en null a propósito: al reservar todavía no hay
        // lavador asignado. Se congela al entregar, que es cuando ya se sabe
        // quién hizo el trabajo.
    }

    public Task<CarwashPublicPaymentConfigResponse> GetPublicPaymentConfigAsync(string slug, CancellationToken cancellationToken = default)
        // Igual que en el Portal de Rentas: no se valida el link acá porque la
        // pasarela tiene que poder cargar aunque todavía no existan credenciales.
        => Task.FromResult(new CarwashPublicPaymentConfigResponse(
            _stripeGateway.IsConfigured ? _stripeGateway.PublishableKey : null,
            PortalPaymentService.SandboxCurrency));

    public async Task<CarwashStripeIntentResponse> CreatePublicStripeIntentAsync(
        string slug, CarwashPaymentIntentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_stripeGateway.IsConfigured)
            throw new AppValidationException("El pago en línea no está disponible. Contacta al negocio.");

        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashServiceItem), request.ServiceId);
        if (service.LandlordId != link.LandlordId || !service.IsActive)
            throw new AppValidationException("El servicio seleccionado no está disponible.");

        if (request.TipAmount < 0m)
            throw new AppValidationException("La propina no puede ser negativa.");

        // El TicketId todavía no existe (el turno se crea recién al confirmar el
        // pago), así que se usa uno descartable sólo para reusar la validación de
        // extras contra el catálogo del negocio.
        var extras = await BuildTicketExtrasAsync(link.LandlordId, Guid.Empty, request.ExtraIds, cancellationToken);

        var tip = request.TipAmount is > 0m
            ? decimal.Round(request.TipAmount.Value, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var amount = CalculateTotal(service.Price, extras) + tip;

        if (amount <= 0m)
            throw new AppValidationException("El monto a pagar debe ser mayor que cero.");

        var result = await _stripeGateway.CreatePaymentIntentAsync(
            PortalPaymentService.ToGatewayAmountInCents(amount), "usd", cancellationToken);

        return new CarwashStripeIntentResponse(
            result.PaymentIntentId, result.ClientSecret, amount, PortalPaymentService.SandboxCurrency);
    }

    public async Task<PublicTicketStatusResponse> GetTicketByTokenAsync(Guid accessToken, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByAccessTokenAsync(accessToken, cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashTicket), accessToken);

        var service = await _serviceRepository.GetByIdAsync(ticket.ServiceId, cancellationToken);
        var landlord = await _landlordRepository.GetByIdAsync(ticket.LandlordId, cancellationToken);
        var extras = await _ticketRepository.GetExtrasByTicketIdsAsync([ticket.Id], cancellationToken);

        return ToPublicStatus(ticket, service?.Name ?? "—", extras, landlord);
    }

    /// <summary>Arma la respuesta pública del turno. El <paramref name="landlord"/> puede venir nulo (negocio borrado): se cae a la marca por defecto en vez de romper la página del cliente.</summary>
    private static PublicTicketStatusResponse ToPublicStatus(
        CarwashTicket ticket,
        string serviceName,
        IReadOnlyList<CarwashTicketExtra> extras,
        Landlord? landlord)
        => new(
            ticket.Id,
            ticket.AccessToken,
            ticket.QueueNumber,
            ticket.Status,
            ticket.ArrivalDeadline,
            ticket.VehiclePlate,
            serviceName,
            ToExtraResponses(extras),
            CalculateTotal(ticket.ServicePrice, extras),
            landlord?.BusinessName ?? "Alkiman",
            landlord?.AppName ?? "Alkiman",
            landlord?.ThemeMode ?? "light",
            landlord?.AccentColor ?? "blue",
            ticket.CreatedAt);

    private async Task<CarwashPortalLink> GetActiveLinkOrThrowAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new NotFoundException(nameof(CarwashPortalLink), slug);

        var link = await _portalLinkRepository.GetBySlugAsync(slug.Trim(), cancellationToken)
            ?? throw new NotFoundException(nameof(CarwashPortalLink), slug);

        if (!link.IsActive)
            throw new NotFoundException(nameof(CarwashPortalLink), slug);

        return link;
    }

    // ============================================================
    // Helpers compartidos
    // ============================================================

    private async Task<bool> IsSoloModeAsync(Guid landlordId, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetByLandlordAsync(landlordId, cancellationToken);
        return settings?.OperationMode == CarwashOperationMode.Solitario;
    }

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>La placa se guarda en mayúsculas y sin espacios para que buscarla y compararla sea predecible.</summary>
    private static string NormalizePlate(string plate)
        => plate.Trim().Replace(" ", string.Empty).ToUpperInvariant();

    /// <summary>
    /// El año es opcional, pero si viene tiene que ser plausible: se aceptan
    /// desde 1900 hasta el año próximo (los modelos nuevos se venden adelantados).
    /// </summary>
    private static int? ValidateVehicleYear(int? year)
    {
        if (year is null)
            return null;

        var maxYear = DateTime.UtcNow.Year + 1;
        if (year < 1900 || year > maxYear)
            throw new AppValidationException($"El año del vehículo debe estar entre 1900 y {maxYear}.");

        return year;
    }

    /// <summary>
    /// Busca al cliente por teléfono (o, si no hay teléfono, por email) dentro del negocio;
    /// si no existe, lo crea al vuelo. Mismo criterio de "buscar-o-crear" que
    /// PortalService.FindOrCreateCustomerAsync (ahí matchea por Email porque el portal de
    /// rentas lo exige; acá el dato más confiable que siempre se pide es el teléfono).
    /// </summary>
    private async Task<Customer> CreateTicketCustomerAsync(
        Guid landlordId, string customerName, string? phone, string? email, string createdBy, DateTime now, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = customerName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CreatedAt = now,
            CreatedBy = createdBy
        };
        await _customerRepository.CreateAsync(customer, cancellationToken);
        return customer;
    }

    /// <summary>Notifica al cliente el cambio de estado del ticket, best-effort: nunca lanza (mismo criterio que RentalRequestService.TryNotifyCustomerAsync). No usa una tabla de historial propia -- reutiliza IEmailSender directamente, sin registrar en EmailMessage (esa tabla es específica de Alquileres).</summary>
    private async Task TryNotifyCustomerAsync(CarwashTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(ticket.CustomerId, cancellationToken);
            if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
                return;

            var subject = $"Tu vehículo {ticket.VehiclePlate} — {StatusLabel(ticket.Status)}";

            var deadlineNote = ticket.Status == CarwashTicketStatus.ArrivalPending && ticket.ArrivalDeadline.HasValue
                ? $"Tienes hasta las <strong>{ticket.ArrivalDeadline:HH:mm}</strong> para llegar."
                : string.Empty;

            var body = EmailTemplate.Build(
                title: "Actualización de estado",
                greeting: $"Hola {customer.FullName},",
                paragraphs: string.IsNullOrEmpty(deadlineNote)
                    ? [$"El estado de tu vehículo <strong>{ticket.VehiclePlate}</strong> cambió a: <strong>{StatusLabel(ticket.Status)}</strong>."]
                    : [$"El estado de tu vehículo <strong>{ticket.VehiclePlate}</strong> cambió a: <strong>{StatusLabel(ticket.Status)}</strong>.", deadlineNote]);

            await _emailSender.SendAsync(customer.Email, customer.FullName, subject, body, cancellationToken);
        }
        catch
        {
            // Best effort: un fallo de notificación nunca debe bloquear el cambio de estado del ticket.
        }
    }

    /// <summary>
    /// Notifica al lavador cuando recibe una propina. Best-effort: nunca lanza.
    /// Solo envía si el lavador tiene email registrado y la propina es > 0.
    /// </summary>
    private async Task TrySendWasherTipNotificationAsync(CarwashTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            if (ticket.TipWasherId is null || (ticket.TipAmount ?? 0) <= 0)
                return;

            var washer = await _washerRepository.GetByIdAsync(ticket.TipWasherId.Value, cancellationToken);
            if (washer is null || string.IsNullOrWhiteSpace(washer.Email))
                return;

            var landlord = await _landlordRepository.GetByIdAsync(ticket.LandlordId, cancellationToken);
            var businessName = landlord?.BusinessName ?? "Alkiman";

            var tip = ticket.TipAmount!.Value;
            var todayTotal = await _ticketRepository.GetTodayTipsByWasherAsync(washer.Id, cancellationToken);

            var html = $"""
                <!DOCTYPE html>
                <html lang="es">
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#f3f4f6;font-family:system-ui,-apple-system,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 16px;">
                    <tr><td align="center">
                      <table width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                        <tr>
                          <td style="background:#059669;padding:28px 32px;text-align:center;">
                            <p style="margin:0;font-size:28px;">💸</p>
                            <p style="margin:8px 0 0;font-size:20px;font-weight:700;color:#ffffff;">¡Recibiste una propina!</p>
                            <p style="margin:4px 0 0;font-size:13px;color:#d1fae5;">{businessName}</p>
                          </td>
                        </tr>

                        <tr>
                          <td style="padding:28px 32px;">
                            <p style="margin:0 0 20px;font-size:15px;color:#374151;">
                              Hola <strong>{washer.FullName.Split(' ')[0]}</strong>, el cliente del turno <strong>#{ticket.QueueNumber}</strong>
                              ({ticket.VehiclePlate}) te dejó una propina:
                            </p>

                            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f0fdf4;border-radius:8px;padding:20px;margin-bottom:20px;">
                              <tr>
                                <td style="font-size:13px;color:#065f46;">Propina este turno</td>
                                <td style="font-size:24px;font-weight:700;color:#059669;text-align:right;">{tip:C}</td>
                              </tr>
                              <tr>
                                <td colspan="2" style="padding-top:12px;border-top:1px solid #bbf7d0;"></td>
                              </tr>
                              <tr>
                                <td style="padding-top:8px;font-size:13px;color:#065f46;">Total hoy</td>
                                <td style="padding-top:8px;font-size:16px;font-weight:600;color:#059669;text-align:right;">{todayTotal:C}</td>
                              </tr>
                            </table>

                            <p style="margin:0;font-size:13px;color:#6b7280;text-align:center;">
                              ¡Sigue así, {washer.FullName.Split(' ')[0]}! 🚗✨
                            </p>
                          </td>
                        </tr>

                        <tr>
                          <td style="background:#f9fafb;padding:16px 32px;text-align:center;border-top:1px solid #e5e7eb;">
                            <p style="margin:0;font-size:12px;color:#9ca3af;">Notificación automática de {businessName}</p>
                          </td>
                        </tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;

            var subject = $"💸 ¡Propina de {tip:C}! — {businessName}";
            await _emailSender.SendAsync(washer.Email, washer.FullName, subject, html, cancellationToken);
        }
        catch
        {
            // Best effort: si falla la notificación, el ticket ya está entregado.
        }
    }

    /// <summary>
    /// Envía la factura/recibo del lavado al cliente cuando el ticket pasa a Delivered.
    /// Best-effort: nunca lanza. Solo envía si el cliente tiene email registrado.
    /// </summary>
    private async Task TrySendInvoiceAsync(CarwashTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(ticket.CustomerId, cancellationToken);
            if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
                return;

            var landlord = await _landlordRepository.GetByIdAsync(ticket.LandlordId, cancellationToken);
            var service  = await _serviceRepository.GetByIdAsync(ticket.ServiceId, cancellationToken);
            var extras   = await _ticketRepository.GetExtrasByTicketIdsAsync([ticket.Id], cancellationToken);

            var businessName = landlord?.BusinessName ?? "Alkiman";
            var serviceName  = service?.Name ?? "Servicio";
            var deliveredAt  = (ticket.DeliveredAt ?? DateTime.UtcNow).ToLocalTime();

            // ── Cálculo de totales ────────────────────────────────────────────────
            var extrasTotal = extras.Sum(e => e.Price);
            var subtotal    = ticket.ServicePrice + extrasTotal;
            var tip         = ticket.TipAmount ?? 0m;
            var total       = subtotal + tip;

            // ── Filas de extras ───────────────────────────────────────────────────
            var extraRows = string.Concat(extras.Select(e =>
                $"""
                <tr>
                  <td style="padding:6px 0;color:#374151;">{e.Name}</td>
                  <td style="padding:6px 0;text-align:right;color:#374151;">{e.Price:C}</td>
                </tr>
                """));

            var tipRow = tip > 0
                ? $"""
                  <tr>
                    <td style="padding:6px 0;color:#374151;">Propina</td>
                    <td style="padding:6px 0;text-align:right;color:#374151;">{tip:C}</td>
                  </tr>
                  """
                : string.Empty;

            // ── Datos del vehículo (solo los que se cargaron) ─────────────────────
            var vehicleDetails = new[]
            {
                ticket.VehicleBrand, ticket.VehicleModel,
                ticket.VehicleYear?.ToString(), ticket.VehicleColor
            };
            var vehicleLine = string.Join(" · ", vehicleDetails.Where(v => !string.IsNullOrWhiteSpace(v)));

            // ── HTML del recibo ───────────────────────────────────────────────────
            var html = $"""
                <!DOCTYPE html>
                <html lang="es">
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#f3f4f6;font-family:system-ui,-apple-system,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 16px;">
                    <tr><td align="center">
                      <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                        <!-- Cabecera -->
                        <tr>
                          <td style="background:#7c3aed;padding:28px 32px;text-align:center;">
                            <p style="margin:0;font-size:22px;font-weight:700;color:#ffffff;">{businessName}</p>
                            <p style="margin:6px 0 0;font-size:13px;color:#ede9fe;">Recibo de servicio de lavado</p>
                          </td>
                        </tr>

                        <!-- Cuerpo -->
                        <tr>
                          <td style="padding:28px 32px;">

                            <!-- Meta -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;">
                              <tr>
                                <td style="font-size:13px;color:#6b7280;">Turno <strong style="color:#111827;">#{ticket.QueueNumber}</strong></td>
                                <td style="font-size:13px;color:#6b7280;text-align:right;">{deliveredAt:dd/MM/yyyy HH:mm}</td>
                              </tr>
                            </table>

                            <!-- Cliente y vehículo -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f9fafb;border-radius:8px;padding:16px;margin-bottom:24px;">
                              <tr>
                                <td style="font-size:13px;color:#6b7280;padding-bottom:4px;">Cliente</td>
                              </tr>
                              <tr>
                                <td style="font-size:15px;font-weight:600;color:#111827;">{customer.FullName}</td>
                              </tr>
                              <tr>
                                <td style="padding-top:12px;font-size:13px;color:#6b7280;padding-bottom:4px;">Vehículo</td>
                              </tr>
                              <tr>
                                <td style="font-size:15px;font-weight:600;color:#111827;letter-spacing:.05em;">{ticket.VehiclePlate}</td>
                              </tr>
                              {(string.IsNullOrWhiteSpace(vehicleLine) ? "" : $"""
                              <tr>
                                <td style="font-size:13px;color:#6b7280;padding-top:2px;">{vehicleLine}</td>
                              </tr>
                              """)}
                            </table>

                            <!-- Detalle de servicios -->
                            <p style="margin:0 0 8px;font-size:13px;font-weight:600;color:#6b7280;text-transform:uppercase;letter-spacing:.05em;">Detalle</p>
                            <table width="100%" cellpadding="0" cellspacing="0" style="border-top:1px solid #e5e7eb;">
                              <tr>
                                <td style="padding:6px 0;color:#374151;">{serviceName}</td>
                                <td style="padding:6px 0;text-align:right;color:#374151;">{ticket.ServicePrice:C}</td>
                              </tr>
                              {extraRows}
                              {tipRow}
                              <tr>
                                <td colspan="2" style="padding-top:8px;border-top:2px solid #e5e7eb;"></td>
                              </tr>
                              <tr>
                                <td style="padding:4px 0;font-size:16px;font-weight:700;color:#111827;">Total</td>
                                <td style="padding:4px 0;text-align:right;font-size:16px;font-weight:700;color:#7c3aed;">{total:C}</td>
                              </tr>
                            </table>

                          </td>
                        </tr>

                        <!-- Pie -->
                        <tr>
                          <td style="background:#f9fafb;padding:20px 32px;text-align:center;border-top:1px solid #e5e7eb;">
                            <p style="margin:0;font-size:13px;color:#6b7280;">¡Gracias por tu preferencia, {customer.FullName.Split(' ')[0]}!</p>
                            <p style="margin:4px 0 0;font-size:12px;color:#9ca3af;">Este recibo fue generado automáticamente por {businessName}.</p>
                          </td>
                        </tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;

            var subject = $"Recibo de lavado — {ticket.VehiclePlate} · {businessName}";
            await _emailSender.SendAsync(customer.Email, customer.FullName, subject, html, cancellationToken);
        }
        catch
        {
            // Best effort: si el envío de la factura falla, el ticket ya está entregado.
        }
    }

    private static string StatusLabel(string status) => status switch
    {
        CarwashTicketStatus.Waiting => "En espera",
        CarwashTicketStatus.ArrivalPending => "Turno llamado, esperando confirmación de llegada",
        CarwashTicketStatus.InProgress => "En lavado",
        CarwashTicketStatus.Drying => "Secando",
        CarwashTicketStatus.Waxing => "Encerando",
        CarwashTicketStatus.Ready => "Listo",
        CarwashTicketStatus.Delivered => "Entregado",
        CarwashTicketStatus.Cancelled => "Cancelado",
        CarwashTicketStatus.Expired => "Expirado",
        _ => status
    };
}
