using System.Text;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Roles;
using Alkiman.Application.Customers;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Modules;
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
    private readonly IModuleProvisioner _moduleProvisioner;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;

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
        IModuleProvisioner moduleProvisioner,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog,
        IEmailSender emailSender)
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
        _moduleProvisioner = moduleProvisioner;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
        _emailSender = emailSender;
    }

    // ============================================================
    // Configuración del módulo
    // ============================================================

    public async Task<CarwashSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var settings = await _settingsRepository.GetByLandlordAsync(landlordId, cancellationToken);
        return new CarwashSettingsResponse(settings?.OperationMode);
    }

    public async Task<CarwashSettingsResponse> SaveSettingsAsync(SaveCarwashSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OperationMode is not (CarwashOperationMode.Empresa or CarwashOperationMode.Solitario))
            throw new AppValidationException($"Modo de operación inválido: '{request.OperationMode}'.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var settings = new CarwashSettings
        {
            LandlordId = landlordId,
            OperationMode = request.OperationMode,
            CreatedAt = now,
            CreatedBy = _currentLandlord.UserId,
            UpdatedAt = now,
            UpdatedBy = _currentLandlord.UserId
        };

        await _settingsRepository.UpsertAsync(settings, cancellationToken);

        // En modo Empresa hace falta a quién asignarle los lavados: se siembra el
        // rol de sistema "Lavador" para que el negocio pueda dar de alta usuarios
        // sin tener que armar el rol y elegir permisos a mano. La definición del rol
        // vive en ModuleProvisioningCatalog; acá solo se decide CUÁNDO crearlo,
        // porque es el módulo el que sabe que en modo Solitario no hace falta.
        if (request.OperationMode == CarwashOperationMode.Empresa)
        {
            await _moduleProvisioner.EnsureSystemRoleAsync(
                landlordId, ModuleCodes.Carwash, SystemRoleNames.Washer, _currentLandlord.UserId, cancellationToken);
        }
        else
        {
            // En modo Solitario el que lava es el propio dueño, así que se le crea su
            // ficha de lavador: sin ella el auto-asignado de AdvanceStatusAsync no
            // tendría a quién apuntar y los turnos quedarían sin nombre en el historial.
            await EnsureWasherForCurrentUserAsync(landlordId, cancellationToken);
        }

        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Settings", landlordId.ToString(), null, settings, cancellationToken);
        return new CarwashSettingsResponse(settings.OperationMode);
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
        await ValidateLinkedUserAsync(request.UserId, landlordId, washerId: null, cancellationToken);

        var washer = new CarwashWasher
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = fullName,
            Phone = Normalize(request.Phone),
            IsActive = true,
            UserId = request.UserId,
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
        await ValidateLinkedUserAsync(request.UserId, washer.LandlordId, washer.Id, cancellationToken);

        washer.FullName = fullName;
        washer.Phone = Normalize(request.Phone);
        washer.IsActive = request.IsActive;
        washer.UserId = request.UserId;
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

    public async Task<IReadOnlyList<CarwashLinkableUserResponse>> GetLinkableUsersAsync(Guid? washerId, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var washers = await _washerRepository.GetAllByLandlordAsync(landlordId, cancellationToken);

        // Una cuenta no puede quedar vinculada a dos lavadores (índice único en
        // CWS_Washers.UserId). Se excluyen las tomadas, salvo la del lavador que
        // se está editando: si no, al abrir el formulario su propia cuenta
        // desaparecería de la lista.
        var taken = washers
            .Where(w => w.UserId is not null && w.Id != washerId)
            .Select(w => w.UserId!.Value)
            .ToHashSet();

        var users = await _userRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return users
            .Where(u => u.IsActive && !taken.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new CarwashLinkableUserResponse(u.Id, u.FullName, u.Email))
            .ToList();
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
    /// Le crea la ficha de lavador a la cuenta que está operando, si no la tiene.
    /// Idempotente: se llama cada vez que se guarda el modo Solitario.
    /// </summary>
    private async Task EnsureWasherForCurrentUserAsync(Guid landlordId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentLandlord.UserId, out var currentUserId))
            return;

        if (await _washerRepository.GetByUserIdAsync(currentUserId, cancellationToken) is not null)
            return;

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null || user.LandlordId != landlordId)
            return;

        var washer = new CarwashWasher
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = user.FullName,
            IsActive = true,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _washerRepository.CreateAsync(washer, cancellationToken);
    }

    /// <summary>La cuenta a vincular tiene que ser del mismo negocio, estar activa y no pertenecer ya a otro lavador.</summary>
    private async Task ValidateLinkedUserAsync(Guid? userId, Guid landlordId, Guid? washerId, CancellationToken cancellationToken)
    {
        if (userId is not { } id)
            return;

        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        if (user.LandlordId != landlordId || !user.IsActive)
            throw new AppValidationException("La cuenta seleccionada no está disponible.");

        var existing = await _washerRepository.GetByUserIdAsync(id, cancellationToken);
        if (existing is not null && existing.Id != washerId)
            throw new AppValidationException($"La cuenta ya está vinculada al lavador '{existing.FullName}'.");
    }

    private async Task<CarwashWasherResponse> ToWasherResponseAsync(CarwashWasher washer, CancellationToken cancellationToken)
    {
        string? userEmail = null;
        if (washer.UserId is { } userId)
            userEmail = (await _userRepository.GetByIdAsync(userId, cancellationToken))?.Email;

        var canDelete = !await _washerRepository.HasTicketsAsync(washer.Id, cancellationToken);

        return new CarwashWasherResponse(
            washer.Id, washer.FullName, washer.Phone, washer.IsActive, washer.UserId, userEmail, canDelete);
    }

    /// <summary>
    /// El lavador vinculado a la cuenta que está operando, si tiene uno. Es lo que
    /// permite que en modo Solitario el ticket quede a nombre de quien lo lava sin
    /// pedirle un paso extra.
    /// </summary>
    private async Task<CarwashWasher?> GetCurrentUserWasherAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentLandlord.UserId, out var currentUserId))
            return null;

        return await _washerRepository.GetByUserIdAsync(currentUserId, cancellationToken);
    }

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
        var customer = await FindOrCreateCustomerAsync(
            landlordId, request.CustomerName, request.CustomerPhone, request.CustomerEmail, _currentLandlord.UserId, now, cancellationToken);
        var queueNumber = await _ticketRepository.GetNextQueueNumberAsync(landlordId, cancellationToken);

        var ticket = new CarwashTicket
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            CustomerId = customer.Id,
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
                // En modo Solitario no hay a quién asignarle el trabajo: el ticket
                // queda a nombre de quien arranca el lavado, para que el historial y
                // las métricas por persona funcionen igual que en modo Empresa sin
                // pedirle al usuario un paso extra.
                //
                // Requiere que la cuenta tenga un lavador vinculado; en modo Solitario
                // se le crea al elegir el modo (ver SaveSettingsAsync).
                if (ticket.AssignedToWasherId is null && await IsSoloModeAsync(ticket.LandlordId, cancellationToken))
                {
                    var currentWasher = await GetCurrentUserWasherAsync(cancellationToken);
                    if (currentWasher is not null && currentWasher.LandlordId == ticket.LandlordId)
                        ticket.AssignedToWasherId = currentWasher.Id;
                }
                break;
            case CarwashTicketStatus.Ready:
                ticket.ReadyAt = now;
                break;
            case CarwashTicketStatus.Delivered:
                ticket.DeliveredAt = now;
                break;
        }
        ticket.UpdatedAt = now;
        ticket.UpdatedBy = _currentLandlord.UserId;

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CWS_Tickets", ticket.Id.ToString(), null, ticket, cancellationToken);

        await TryNotifyCustomerAsync(ticket, cancellationToken);

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
        var customer = await _customerRepository.GetByIdAsync(ticket.CustomerId, cancellationToken);
        var service = await _serviceRepository.GetByIdAsync(ticket.ServiceId, cancellationToken);

        string? assignedToName = null;
        if (ticket.AssignedToWasherId is { } assignedId)
            assignedToName = (await _washerRepository.GetByIdAsync(assignedId, cancellationToken))?.FullName;

        return new CarwashTicketResponse(
            ticket.Id, ticket.QueueNumber, ticket.VehiclePlate,
            ticket.VehicleBrand, ticket.VehicleModel, ticket.VehicleYear, ticket.VehicleColor,
            ticket.CustomerId, customer?.FullName ?? "—", customer?.Phone,
            ticket.ServiceId, service?.Name ?? "—", ticket.ServicePrice,
            ToExtraResponses(extras), CalculateTotal(ticket.ServicePrice, extras),
            ticket.AssignedToWasherId, assignedToName,
            ticket.Status, ticket.Source,
            ticket.ArrivalDeadline, ticket.ArrivedAt, ticket.StartedAt, ticket.ReadyAt, ticket.DeliveredAt, ticket.CancelledAt,
            ticket.Notes, ticket.CreatedAt);
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

        return new PublicCarwashLinkResponse(
            link.Title,
            landlord?.BusinessName ?? "Alkiman",
            landlord?.AppName ?? "Alkiman",
            landlord?.ThemeMode ?? "light",
            landlord?.AccentColor ?? "blue",
            activeServices,
            activeExtras);
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
        var customer = await FindOrCreateCustomerAsync(
            link.LandlordId, request.CustomerName, request.CustomerPhone, request.CustomerEmail, PortalCreatedBy, now, cancellationToken);
        var queueNumber = await _ticketRepository.GetNextQueueNumberAsync(link.LandlordId, cancellationToken);

        var ticket = new CarwashTicket
        {
            Id = Guid.NewGuid(),
            LandlordId = link.LandlordId,
            CustomerId = customer.Id,
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

        await _ticketRepository.CreateAsync(ticket, cancellationToken);
        await _ticketRepository.AddExtrasAsync(extras, cancellationToken);

        var landlord = await _landlordRepository.GetByIdAsync(link.LandlordId, cancellationToken);
        return ToPublicStatus(ticket, service.Name, extras, landlord);
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
    private async Task<Customer> FindOrCreateCustomerAsync(
        Guid landlordId, string customerName, string? phone, string? email, string createdBy, DateTime now, CancellationToken cancellationToken)
    {
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        var existingCustomers = await _customerRepository.GetAllByLandlordAsync(landlordId, cancellationToken);

        Customer? existing = null;
        if (normalizedPhone is not null)
        {
            existing = existingCustomers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Phone) && string.Equals(c.Phone.Trim(), normalizedPhone, StringComparison.OrdinalIgnoreCase));
        }
        if (existing is null && normalizedEmail is not null)
        {
            existing = existingCustomers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Email) && string.Equals(c.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase));
        }

        if (existing is not null)
            return existing;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = customerName.Trim(),
            Phone = normalizedPhone,
            Email = normalizedEmail,
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
            var body = $"Hola {customer.FullName}, el estado de tu vehículo ({ticket.VehiclePlate}) cambió a: {StatusLabel(ticket.Status)}.";
            if (ticket.Status == CarwashTicketStatus.ArrivalPending && ticket.ArrivalDeadline.HasValue)
                body += $" Tenés hasta las {ticket.ArrivalDeadline:HH:mm} para llegar.";

            await _emailSender.SendAsync(customer.Email, customer.FullName, subject, body, cancellationToken);
        }
        catch
        {
            // Best effort: un fallo de notificación nunca debe bloquear el cambio de estado del ticket.
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
