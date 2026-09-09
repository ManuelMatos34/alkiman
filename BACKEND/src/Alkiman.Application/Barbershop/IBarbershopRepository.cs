using BarbershopAppointmentEntity = Alkiman.Domain.Entities.BarbershopAppointment;
using BarbershopPortalLinkEntity = Alkiman.Domain.Entities.BarbershopPortalLink;
using BarbershopServiceEntity = Alkiman.Domain.Entities.BarbershopService;
using BarbershopStylistEntity = Alkiman.Domain.Entities.BarbershopStylist;

namespace Alkiman.Application.Barbershop;

public interface IBarbershopRepository
{
    // --- Services ---
    Task<IReadOnlyList<BarbershopServiceEntity>> GetServicesByLandlordAsync(Guid landlordId, CancellationToken ct = default);
    Task<BarbershopServiceEntity?> GetServiceByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateServiceAsync(BarbershopServiceEntity entity, CancellationToken ct = default);
    Task UpdateServiceAsync(BarbershopServiceEntity entity, CancellationToken ct = default);
    Task DeleteServiceAsync(int id, CancellationToken ct = default);
    Task<bool> ServiceHasAppointmentsAsync(int id, CancellationToken ct = default);

    // --- Stylists ---
    Task<IReadOnlyList<BarbershopStylistEntity>> GetStylistsByLandlordAsync(Guid landlordId, CancellationToken ct = default);
    Task<BarbershopStylistEntity?> GetStylistByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateStylistAsync(BarbershopStylistEntity entity, CancellationToken ct = default);
    Task UpdateStylistAsync(BarbershopStylistEntity entity, CancellationToken ct = default);
    Task DeleteStylistAsync(Guid id, CancellationToken ct = default);
    Task<bool> StylistHasAppointmentsAsync(Guid id, CancellationToken ct = default);

    // --- Portal links ---
    Task<IReadOnlyList<BarbershopPortalLinkEntity>> GetPortalLinksByLandlordAsync(Guid landlordId, CancellationToken ct = default);
    Task<BarbershopPortalLinkEntity?> GetPortalLinkByIdAsync(Guid id, CancellationToken ct = default);
    Task<BarbershopPortalLinkEntity?> GetPortalLinkBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<Guid> CreatePortalLinkAsync(BarbershopPortalLinkEntity entity, CancellationToken ct = default);
    Task UpdatePortalLinkAsync(BarbershopPortalLinkEntity entity, CancellationToken ct = default);
    Task DeletePortalLinkAsync(Guid id, CancellationToken ct = default);

    // --- Appointments ---
    Task<IReadOnlyList<BarbershopAppointmentEntity>> GetAppointmentsByLandlordAsync(Guid landlordId, DateTime? date, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<BarbershopAppointmentEntity?> GetAppointmentByIdAsync(Guid id, CancellationToken ct = default);
    Task<BarbershopAppointmentEntity?> GetAppointmentByTokenAsync(string token, CancellationToken ct = default);
    Task<Guid> CreateAppointmentAsync(BarbershopAppointmentEntity entity, CancellationToken ct = default);
    Task UpdateAppointmentAsync(BarbershopAppointmentEntity entity, CancellationToken ct = default);

    // --- Metrics ---
    Task<IReadOnlyList<BarbershopAppointmentEntity>> GetAppointmentsForMetricsAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken ct = default);

    // --- Lookup helpers ---
    Task<BarbershopStylistEntity?> GetStylistByAppointmentLandlordAsync(Guid stylistId, Guid landlordId, CancellationToken ct = default);
    Task<BarbershopServiceEntity?> GetServiceByAppointmentLandlordAsync(int serviceId, Guid landlordId, CancellationToken ct = default);
}
