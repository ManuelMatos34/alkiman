using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.WhatsApp;
using Alkiman.Domain.Enums;
using Microsoft.Extensions.Configuration;

// Entity aliases to avoid name collision between the Application service class
// 'BarbershopService' and the Domain entity 'Alkiman.Domain.Entities.BarbershopService'.
using BarbershopAppointmentEntity = Alkiman.Domain.Entities.BarbershopAppointment;
using BarbershopPortalLinkEntity = Alkiman.Domain.Entities.BarbershopPortalLink;
using BarbershopServiceEntity = Alkiman.Domain.Entities.BarbershopService;
using BarbershopStylistEntity = Alkiman.Domain.Entities.BarbershopStylist;

namespace Alkiman.Application.Barbershop;

/// <summary>
/// Gestión del módulo Barbería: catálogo de servicios, directorio de estilistas,
/// links de portal público y el ciclo de vida de las citas (alta manual, avance
/// de estado, cancelación), más los flujos públicos (agendamiento por slug y
/// consulta de estado por token de seguimiento).
/// Todos los métodos autenticados se resuelven contra el negocio actual
/// (<see cref="ICurrentLandlordService"/>); los públicos SOLO se resuelven
/// por Slug/TrackingToken, nunca por un LandlordId enviado por el caller.
/// </summary>
public class BarbershopService : IBarbershopService
{
    private const string PortalCreatedBy = "barbershop-portal";
    private const int DefaultWindowDays = 30;

    /// <summary>Transición lineal de estados: Scheduled → Confirmed → InProgress → Completed.</summary>
    private static readonly Dictionary<string, string> NextStatus = new()
    {
        ["Scheduled"]  = "Confirmed",
        ["Confirmed"]  = "InProgress",
        ["InProgress"] = "Completed",
    };

    private readonly IBarbershopRepository _repository;
    private readonly ILandlordRepository _landlordRepository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IConfiguration _configuration;

    public BarbershopService(
        IBarbershopRepository repository,
        ILandlordRepository landlordRepository,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog,
        IEmailSender emailSender,
        IWhatsAppSender whatsAppSender,
        IConfiguration configuration)
    {
        _repository = repository;
        _landlordRepository = landlordRepository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _configuration = configuration;
    }

    // ============================================================
    // Catálogo de servicios
    // ============================================================

    public async Task<IReadOnlyList<BarbershopServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var services = await _repository.GetServicesByLandlordAsync(landlordId, cancellationToken);
        return services.Select(s => ToServiceResponse(s)).ToList();
    }

    public async Task<BarbershopServiceResponse> CreateServiceAsync(CreateBarbershopServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateServiceRequest(request.Name, request.Price, request.DurationMinutes);

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var entity = new BarbershopServiceEntity
        {
            LandlordId = landlordId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        entity.Id = await _repository.CreateServiceAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "BRB_Services", entity.Id.ToString(), null, entity, cancellationToken);
        return ToServiceResponse(entity);
    }

    public async Task<BarbershopServiceResponse> UpdateServiceAsync(int id, UpdateBarbershopServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateServiceRequest(request.Name, request.Price, request.DurationMinutes);

        var entity = await GetOwnedServiceOrThrowAsync(id, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Price = request.Price;
        entity.DurationMinutes = request.DurationMinutes;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateServiceAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_Services", entity.Id.ToString(), null, entity, cancellationToken);
        return ToServiceResponse(entity);
    }

    public async Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedServiceOrThrowAsync(id, cancellationToken);

        if (await _repository.ServiceHasAppointmentsAsync(id, cancellationToken))
            throw new AppValidationException("No se puede eliminar un servicio que ya fue usado en citas. Desactívalo en su lugar.");

        await _repository.DeleteServiceAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "BRB_Services", entity.Id.ToString(), entity, null, cancellationToken);
    }

    private static void ValidateServiceRequest(string name, decimal price, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AppValidationException("El nombre del servicio es obligatorio.");
        if (price < 0)
            throw new AppValidationException("El precio no puede ser negativo.");
        if (durationMinutes <= 0)
            throw new AppValidationException("La duración debe ser mayor a cero minutos.");
    }

    private async Task<BarbershopServiceEntity> GetOwnedServiceOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var entity = await _repository.GetServiceByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopServiceEntity), id);

        if (entity.LandlordId != landlordId)
            throw new ForbiddenException("El servicio no pertenece al negocio autenticado.");

        return entity;
    }

    private static BarbershopServiceResponse ToServiceResponse(BarbershopServiceEntity s) => new(
        s.Id, s.Name, s.Description, s.Price, s.DurationMinutes, s.IsActive);

    // ============================================================
    // Directorio de estilistas
    // ============================================================

    public async Task<IReadOnlyList<BarbershopStylistResponse>> GetStylistsAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var stylists = await _repository.GetStylistsByLandlordAsync(landlordId, cancellationToken);
        return stylists.Select(s => ToStylistResponse(s)).ToList();
    }

    public async Task<BarbershopStylistResponse> CreateStylistAsync(CreateBarbershopStylistRequest request, CancellationToken cancellationToken = default)
    {
        var fullName = ValidateStylistName(request.FullName);
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var entity = new BarbershopStylistEntity
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

        await _repository.CreateStylistAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "BRB_Stylists", entity.Id.ToString(), null, entity, cancellationToken);
        return ToStylistResponse(entity);
    }

    public async Task<BarbershopStylistResponse> UpdateStylistAsync(Guid id, UpdateBarbershopStylistRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedStylistOrThrowAsync(id, cancellationToken);
        var fullName = ValidateStylistName(request.FullName);

        entity.FullName = fullName;
        entity.Phone = Normalize(request.Phone);
        entity.Email = Normalize(request.Email);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateStylistAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_Stylists", entity.Id.ToString(), null, entity, cancellationToken);
        return ToStylistResponse(entity);
    }

    public async Task DeleteStylistAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedStylistOrThrowAsync(id, cancellationToken);

        if (await _repository.StylistHasAppointmentsAsync(id, cancellationToken))
            throw new AppValidationException("Este estilista ya tiene citas registradas. Desactivalo en vez de eliminarlo para no perder el historial.");

        await _repository.DeleteStylistAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "BRB_Stylists", entity.Id.ToString(), entity, null, cancellationToken);
    }

    private static string ValidateStylistName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new AppValidationException("El nombre del estilista es obligatorio.");
        return fullName.Trim();
    }

    private async Task<BarbershopStylistEntity> GetOwnedStylistOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var entity = await _repository.GetStylistByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopStylistEntity), id);

        if (entity.LandlordId != landlordId)
            throw new ForbiddenException("El estilista no pertenece al negocio autenticado.");

        return entity;
    }

    private static BarbershopStylistResponse ToStylistResponse(BarbershopStylistEntity s) => new(
        s.Id, s.FullName, s.Phone, s.Email, s.IsActive);

    // ============================================================
    // Links de portal
    // ============================================================

    public async Task<IReadOnlyList<BarbershopPortalLinkResponse>> GetPortalLinksAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var links = await _repository.GetPortalLinksByLandlordAsync(landlordId, cancellationToken);
        var stylists = await _repository.GetStylistsByLandlordAsync(landlordId, cancellationToken);
        var stylistMap = stylists.ToDictionary(s => s.Id, s => s.FullName);

        return links.Select(l => ToPortalLinkResponse(l, stylistMap)).ToList();
    }

    public async Task<BarbershopPortalLinkResponse> CreatePortalLinkAsync(CreateBarbershopPortalLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 150)
            throw new AppValidationException("El título del link es inválido.");

        ValidateSlugFormat(request.Slug);

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        if (await _repository.SlugExistsAsync(request.Slug.Trim(), cancellationToken))
            throw new AppValidationException("Ya existe un link con ese identificador. Elije uno diferente.");

        // Validate stylist belongs to landlord if provided
        if (request.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(request.StylistId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(BarbershopStylistEntity), request.StylistId.Value);
            if (stylist.LandlordId != landlordId)
                throw new ForbiddenException("El estilista no pertenece al negocio autenticado.");
        }

        var link = new BarbershopPortalLinkEntity
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            StylistId = request.StylistId,
            Title = request.Title.Trim(),
            Slug = request.Slug.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreatePortalLinkAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "BRB_PortalLinks", link.Id.ToString(), null, link, cancellationToken);

        string? stylistName = null;
        if (link.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(link.StylistId.Value, cancellationToken);
            stylistName = stylist?.FullName;
        }

        return new BarbershopPortalLinkResponse(link.Id, link.StylistId, stylistName, link.Title, link.Slug, link.IsActive, link.CreatedAt);
    }

    public async Task<BarbershopPortalLinkResponse> SetPortalLinkActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedPortalLinkOrThrowAsync(id, cancellationToken);
        link.IsActive = isActive;
        link.UpdatedAt = DateTime.UtcNow;
        link.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdatePortalLinkAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_PortalLinks", link.Id.ToString(), null, link, cancellationToken);

        string? stylistName = null;
        if (link.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(link.StylistId.Value, cancellationToken);
            stylistName = stylist?.FullName;
        }

        return new BarbershopPortalLinkResponse(link.Id, link.StylistId, stylistName, link.Title, link.Slug, link.IsActive, link.CreatedAt);
    }

    public async Task DeletePortalLinkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedPortalLinkOrThrowAsync(id, cancellationToken);
        await _repository.DeletePortalLinkAsync(link.Id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "BRB_PortalLinks", link.Id.ToString(), link, null, cancellationToken);
    }

    private async Task<BarbershopPortalLinkEntity> GetOwnedPortalLinkOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var link = await _repository.GetPortalLinkByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopPortalLinkEntity), id);

        if (link.LandlordId != landlordId)
            throw new ForbiddenException("El link no pertenece al negocio autenticado.");

        return link;
    }

    private static BarbershopPortalLinkResponse ToPortalLinkResponse(BarbershopPortalLinkEntity link, Dictionary<Guid, string> stylistMap)
    {
        string? stylistName = link.StylistId.HasValue && stylistMap.TryGetValue(link.StylistId.Value, out var name) ? name : null;
        return new BarbershopPortalLinkResponse(link.Id, link.StylistId, stylistName, link.Title, link.Slug, link.IsActive, link.CreatedAt);
    }

    /// <summary>El slug del portal solo puede tener letras minúsculas, números y guiones.</summary>
    private static void ValidateSlugFormat(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 100)
            throw new AppValidationException("El identificador del link es inválido.");

        foreach (var ch in slug.Trim())
        {
            if (!char.IsLower(ch) && !char.IsDigit(ch) && ch != '-')
                throw new AppValidationException("El identificador solo puede contener letras minúsculas, números y guiones.");
        }
    }

    // ============================================================
    // Tablero / Citas
    // ============================================================

    public async Task<IReadOnlyList<BarbershopAppointmentResponse>> GetAppointmentsAsync(DateTime? date, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var appointments = await _repository.GetAppointmentsByLandlordAsync(landlordId, date, from, to, cancellationToken);

        var stylistMap = new Dictionary<Guid, string>();
        var serviceMap = new Dictionary<int, (string Name, decimal Price)>();

        var allStylists = await _repository.GetStylistsByLandlordAsync(landlordId, cancellationToken);
        var allServices = await _repository.GetServicesByLandlordAsync(landlordId, cancellationToken);

        foreach (var s in allStylists) stylistMap[s.Id] = s.FullName;
        foreach (var s in allServices) serviceMap[s.Id] = (s.Name, s.Price);

        return appointments.Select(a => ToAppointmentResponse(a, stylistMap, serviceMap)).ToList();
    }

    public async Task<BarbershopAppointmentResponse> CreateAppointmentAsync(CreateBarbershopAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName))
            throw new AppValidationException("El nombre del cliente es obligatorio.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        // Validate stylist
        if (request.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(request.StylistId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(BarbershopStylistEntity), request.StylistId.Value);
            if (stylist.LandlordId != landlordId || !stylist.IsActive)
                throw new AppValidationException("El estilista seleccionado no está disponible.");
        }

        // Validate service
        if (request.ServiceId.HasValue)
        {
            var service = await _repository.GetServiceByIdAsync(request.ServiceId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(BarbershopServiceEntity), request.ServiceId.Value);
            if (service.LandlordId != landlordId || !service.IsActive)
                throw new AppValidationException("El servicio seleccionado no está disponible.");
        }

        var entity = new BarbershopAppointmentEntity
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            StylistId = request.StylistId,
            ServiceId = request.ServiceId,
            TrackingToken = Guid.NewGuid().ToString("N"),
            ClientName = request.ClientName.Trim(),
            ClientPhone = Normalize(request.ClientPhone),
            ClientEmail = Normalize(request.ClientEmail),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ScheduledAt = request.ScheduledAt,
            Source = "Manual",
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreateAppointmentAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "BRB_Appointments", entity.Id.ToString(), null, entity, cancellationToken);

        return await BuildAppointmentResponseAsync(entity, cancellationToken);
    }

    public async Task<BarbershopAppointmentResponse> AdvanceStatusAsync(Guid id, bool isPaid, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAppointmentOrThrowAsync(id, cancellationToken);

        if (!NextStatus.TryGetValue(entity.Status, out var next))
            throw new AppValidationException($"No se puede avanzar una cita en estado '{entity.Status}'.");

        entity.Status = next;
        if (next == "Completed") entity.IsPaid = isPaid;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAppointmentAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_Appointments", entity.Id.ToString(), null, entity, cancellationToken);

        return await BuildAppointmentResponseAsync(entity, cancellationToken);
    }

    public async Task<BarbershopAppointmentResponse> MarkAsPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAppointmentOrThrowAsync(id, cancellationToken);

        if (entity.Status != "Completed")
            throw new AppValidationException("Solo se pueden marcar como cobradas las citas completadas.");

        entity.IsPaid = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAppointmentAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_Appointments", entity.Id.ToString(), null, entity, cancellationToken);

        return await BuildAppointmentResponseAsync(entity, cancellationToken);
    }

    public async Task CancelAppointmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAppointmentOrThrowAsync(id, cancellationToken);

        if (entity.Status is "Completed" or "Cancelled")
            throw new AppValidationException("Esta cita ya fue finalizada y no se puede cancelar.");

        entity.Status = "Cancelled";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAppointmentAsync(entity, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "BRB_Appointments", entity.Id.ToString(), null, entity, cancellationToken);
    }

    private async Task<BarbershopAppointmentEntity> GetOwnedAppointmentOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var entity = await _repository.GetAppointmentByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopAppointmentEntity), id);

        if (entity.LandlordId != landlordId)
            throw new ForbiddenException("La cita no pertenece al negocio autenticado.");

        return entity;
    }

    private async Task<BarbershopAppointmentResponse> BuildAppointmentResponseAsync(BarbershopAppointmentEntity entity, CancellationToken cancellationToken)
    {
        string? stylistName = null;
        if (entity.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(entity.StylistId.Value, cancellationToken);
            stylistName = stylist?.FullName;
        }

        string? serviceName = null;
        decimal? servicePrice = null;
        if (entity.ServiceId.HasValue)
        {
            var service = await _repository.GetServiceByIdAsync(entity.ServiceId.Value, cancellationToken);
            serviceName = service?.Name;
            servicePrice = service?.Price;
        }

        return new BarbershopAppointmentResponse(
            entity.Id, entity.StylistId, stylistName, entity.ServiceId, serviceName, servicePrice,
            entity.ClientName, entity.ClientPhone, entity.ClientEmail, entity.Notes,
            entity.ScheduledAt, entity.Source, entity.Status, entity.IsPaid, entity.CreatedAt);
    }

    private static BarbershopAppointmentResponse ToAppointmentResponse(
        BarbershopAppointmentEntity a,
        Dictionary<Guid, string> stylistMap,
        Dictionary<int, (string Name, decimal Price)> serviceMap)
    {
        string? stylistName = a.StylistId.HasValue && stylistMap.TryGetValue(a.StylistId.Value, out var sn) ? sn : null;
        string? serviceName = null;
        decimal? servicePrice = null;
        if (a.ServiceId.HasValue && serviceMap.TryGetValue(a.ServiceId.Value, out var svc))
        {
            serviceName = svc.Name;
            servicePrice = svc.Price;
        }
        return new BarbershopAppointmentResponse(
            a.Id, a.StylistId, stylistName, a.ServiceId, serviceName, servicePrice,
            a.ClientName, a.ClientPhone, a.ClientEmail, a.Notes,
            a.ScheduledAt, a.Source, a.Status, a.IsPaid, a.CreatedAt);
    }

    // ============================================================
    // Métricas
    // ============================================================

    public async Task<BarbershopMetricsResponse> GetMetricsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var fromDate = from?.Date ?? today.AddDays(-DefaultWindowDays);
        var toDate = to?.Date ?? today;

        var appointments = await _repository.GetAppointmentsForMetricsAsync(landlordId, fromDate, toDate.AddDays(1), cancellationToken);

        var total = appointments.Count;
        var completed = appointments.Count(a => a.Status == "Completed");
        var cancelled = appointments.Count(a => a.Status == "Cancelled");

        var allServices = await _repository.GetServicesByLandlordAsync(landlordId, cancellationToken);
        var serviceMap = allServices.ToDictionary(s => s.Id);

        var totalRevenue = appointments
            .Where(a => a.Status == "Completed" && a.ServiceId.HasValue && serviceMap.ContainsKey(a.ServiceId.Value))
            .Sum(a => serviceMap[a.ServiceId!.Value].Price);

        var daily = appointments
            .GroupBy(a => DateOnly.FromDateTime(a.ScheduledAt.Date))
            .Select(g => new BarbershopDailyPoint(
                g.Key,
                g.Count(),
                g.Where(a => a.Status == "Completed" && a.ServiceId.HasValue && serviceMap.ContainsKey(a.ServiceId!.Value))
                 .Sum(a => serviceMap[a.ServiceId!.Value].Price)))
            .OrderBy(d => d.Date)
            .ToList();

        var topServices = appointments
            .Where(a => a.ServiceId.HasValue && serviceMap.ContainsKey(a.ServiceId.Value))
            .GroupBy(a => serviceMap[a.ServiceId!.Value].Name)
            .Select(g => new BarbershopServiceUsage(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .Take(8)
            .ToList();

        var allStylists = await _repository.GetStylistsByLandlordAsync(landlordId, cancellationToken);
        var stylistMap = allStylists.ToDictionary(s => s.Id);

        var stylistRanking = appointments
            .Where(a => a.StylistId.HasValue && stylistMap.ContainsKey(a.StylistId.Value))
            .GroupBy(a => stylistMap[a.StylistId!.Value].FullName)
            .Select(g => new BarbershopStylistRanking(
                g.Key,
                g.Count(),
                g.Where(a => a.Status == "Completed" && a.ServiceId.HasValue && serviceMap.ContainsKey(a.ServiceId!.Value))
                 .Sum(a => serviceMap[a.ServiceId!.Value].Price)))
            .OrderByDescending(s => s.Count)
            .ToList();

        return new BarbershopMetricsResponse(total, completed, cancelled, totalRevenue, daily, topServices, stylistRanking);
    }

    // ============================================================
    // Disponibilidad de slots
    // ============================================================

    public async Task<IReadOnlyList<BarbershopSlotResponse>> GetSlotsAsync(DateTime date, int? serviceId, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        return await ComputeSlotsAsync(landlordId, date, serviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<BarbershopSlotResponse>> GetPublicSlotsAsync(string slug, DateTime date, int? serviceId, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);
        return await ComputeSlotsAsync(link.LandlordId, date, serviceId, cancellationToken);
    }

    private async Task<IReadOnlyList<BarbershopSlotResponse>> ComputeSlotsAsync(
        Guid landlordId, DateTime date, int? serviceId, CancellationToken cancellationToken)
    {
        const int businessStartHour = 8;
        const int businessEndHour = 20;
        const int slotStepMinutes = 30;
        const int defaultDurationMinutes = 30;

        // Resolve requested service duration
        int requestedDuration = defaultDurationMinutes;
        if (serviceId.HasValue)
        {
            var svc = await _repository.GetServiceByIdAsync(serviceId.Value, cancellationToken);
            if (svc != null && svc.LandlordId == landlordId)
                requestedDuration = svc.DurationMinutes;
        }

        // Fetch existing appointments for the day (non-cancelled)
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        var appointments = await _repository.GetAppointmentsByLandlordAsync(landlordId, date.Date, null, null, cancellationToken);
        var activeAppointments = appointments.Where(a => a.Status != "Cancelled").ToList();

        // Build a service duration map for occupied-range calculation
        var allServices = await _repository.GetServicesByLandlordAsync(landlordId, cancellationToken);
        var serviceDurationMap = allServices.ToDictionary(s => s.Id, s => s.DurationMinutes);

        // Generate slot grid
        var slots = new List<BarbershopSlotResponse>();
        var businessStart = dayStart.AddHours(businessStartHour);
        var businessEnd = dayStart.AddHours(businessEndHour);
        var lastSlotStart = businessEnd.AddMinutes(-requestedDuration);

        var current = businessStart;
        while (current <= lastSlotStart)
        {
            var slotEnd = current.AddMinutes(requestedDuration);

            bool isTaken = activeAppointments.Any(a =>
            {
                int apptDuration = a.ServiceId.HasValue && serviceDurationMap.TryGetValue(a.ServiceId.Value, out var d)
                    ? d : defaultDurationMinutes;
                var occupiedEnd = a.ScheduledAt.AddMinutes(apptDuration);
                return current < occupiedEnd && slotEnd > a.ScheduledAt;
            });

            slots.Add(new BarbershopSlotResponse(current, !isTaken));
            current = current.AddMinutes(slotStepMinutes);
        }

        return slots;
    }

    // ============================================================
    // Público (sin login, resuelto por Slug/TrackingToken)
    // ============================================================

    public async Task<BarbershopPublicLinkResponse> GetPublicLinkAsync(string slug, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);
        var services = await _repository.GetServicesByLandlordAsync(link.LandlordId, cancellationToken);
        var activeServices = services.Where(s => s.IsActive).Select(s => ToServiceResponse(s)).ToList();

        string? stylistName = null;
        if (link.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(link.StylistId.Value, cancellationToken);
            stylistName = stylist?.FullName;
        }

        return new BarbershopPublicLinkResponse(link.Title, link.IsActive, link.StylistId, stylistName, activeServices);
    }

    public async Task<BarbershopBookingResponse> BookBySlugAsync(string slug, BookBarbershopAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.ClientName))
            throw new AppValidationException("El nombre es obligatorio.");

        BarbershopServiceEntity? service = null;
        if (request.ServiceId.HasValue)
        {
            service = await _repository.GetServiceByIdAsync(request.ServiceId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(BarbershopServiceEntity), request.ServiceId.Value);
            if (service.LandlordId != link.LandlordId || !service.IsActive)
                throw new AppValidationException("El servicio seleccionado no está disponible.");
        }

        var entity = new BarbershopAppointmentEntity
        {
            Id = Guid.NewGuid(),
            LandlordId = link.LandlordId,
            StylistId = link.StylistId,
            ServiceId = request.ServiceId,
            PortalLinkId = link.Id,
            TrackingToken = Guid.NewGuid().ToString("N"),
            ClientName = request.ClientName.Trim(),
            ClientPhone = Normalize(request.ClientPhone),
            ClientEmail = Normalize(request.ClientEmail),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ScheduledAt = request.ScheduledAt,
            Source = "Portal",
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = PortalCreatedBy
        };

        await _repository.CreateAppointmentAsync(entity, cancellationToken);

        await TrySendBookingConfirmationAsync(entity, service, link, cancellationToken);

        return new BarbershopBookingResponse(entity.Id, entity.TrackingToken, entity.ScheduledAt);
    }

    public async Task<BarbershopAppointmentStatusResponse> GetAppointmentByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAppointmentByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopAppointmentEntity), token);

        string? stylistName = null;
        if (entity.StylistId.HasValue)
        {
            var stylist = await _repository.GetStylistByIdAsync(entity.StylistId.Value, cancellationToken);
            stylistName = stylist?.FullName;
        }

        string? serviceName = null;
        if (entity.ServiceId.HasValue)
        {
            var service = await _repository.GetServiceByIdAsync(entity.ServiceId.Value, cancellationToken);
            serviceName = service?.Name;
        }

        return new BarbershopAppointmentStatusResponse(entity.Id, entity.ClientName, stylistName, serviceName, entity.ScheduledAt, entity.Status);
    }

    private async Task<BarbershopPortalLinkEntity> GetActiveLinkOrThrowAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new NotFoundException(nameof(BarbershopPortalLinkEntity), slug);

        var link = await _repository.GetPortalLinkBySlugAsync(slug.Trim(), cancellationToken)
            ?? throw new NotFoundException(nameof(BarbershopPortalLinkEntity), slug);

        if (!link.IsActive)
            throw new NotFoundException(nameof(BarbershopPortalLinkEntity), slug);

        return link;
    }

    /// <summary>Envía email de confirmación al cliente si tiene email. Best-effort: nunca lanza.</summary>
    private async Task TrySendBookingConfirmationAsync(
        BarbershopAppointmentEntity entity,
        BarbershopServiceEntity? service,
        BarbershopPortalLinkEntity link,
        CancellationToken cancellationToken)
    {
        try
        {
            var hasPhone = !string.IsNullOrWhiteSpace(entity.ClientPhone);
            var hasEmail = !string.IsNullOrWhiteSpace(entity.ClientEmail);
            if (!hasPhone && !hasEmail) return;

            var landlord = await _landlordRepository.GetByIdAsync(link.LandlordId, cancellationToken);
            var businessName = landlord?.BusinessName ?? "Alkiman";

            string? stylistName = null;
            if (entity.StylistId.HasValue)
            {
                var stylist = await _repository.GetStylistByIdAsync(entity.StylistId.Value, cancellationToken);
                stylistName = stylist?.FullName;
            }

            var serviceName  = service?.Name ?? "—";
            var fecha        = entity.ScheduledAt.ToString("dd/MM/yyyy");
            var hora         = entity.ScheduledAt.ToString("HH:mm");

            // ── WhatsApp (preferido) ──────────────────────────────────────────
            if (hasPhone)
            {
                var parameters = new[] { entity.ClientName.Split(' ')[0], businessName, fecha, hora, serviceName };
                var wa = await _whatsAppSender.SendTemplateAsync(
                    entity.ClientPhone!, WhatsAppTemplates.BarbershopBooking, "es", parameters, cancellationToken);
                if (wa.Success) return;
            }

            // ── Fallback: email ───────────────────────────────────────────────
            if (!hasEmail) return;

            var baseUrl     = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var trackingUrl = $"{baseUrl}/barberia/cita/{entity.TrackingToken}";
            var stylistLine = stylistName != null ? $"💈 Estilista: <strong>{stylistName}</strong>." : string.Empty;

            var subject = $"Tu cita en {businessName} está confirmada";
            var body = EmailTemplate.Build(
                title: "Cita confirmada",
                greeting: $"Hola {entity.ClientName},",
                paragraphs:
                [
                    "Tu cita ha sido registrada. Aquí tienes los detalles:",
                    $"📅 <strong>{fecha}</strong> a las <strong>{hora}</strong><br>" +
                    $"✂️ Servicio: <strong>{serviceName}</strong><br>" +
                    (stylistLine.Length > 0 ? stylistLine : string.Empty),
                    "Puedes consultar el estado de tu cita en cualquier momento usando el botón de abajo."
                ],
                ctaLabel: "Ver estado de mi cita",
                ctaUrl: trackingUrl,
                businessName: businessName);

            await _emailSender.SendAsync(entity.ClientEmail!, entity.ClientName, subject, body, cancellationToken);
        }
        catch
        {
            // Best effort: un fallo de notificación nunca debe bloquear el agendamiento.
        }
    }

    // ============================================================
    // Helpers compartidos
    // ============================================================

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
