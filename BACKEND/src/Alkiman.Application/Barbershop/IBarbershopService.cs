namespace Alkiman.Application.Barbershop;

public interface IBarbershopService
{
    // --- Catálogo ---
    Task<IReadOnlyList<BarbershopServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<BarbershopServiceResponse> CreateServiceAsync(CreateBarbershopServiceRequest request, CancellationToken cancellationToken = default);
    Task<BarbershopServiceResponse> UpdateServiceAsync(int id, UpdateBarbershopServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default);

    // --- Estilistas ---
    Task<IReadOnlyList<BarbershopStylistResponse>> GetStylistsAsync(CancellationToken cancellationToken = default);
    Task<BarbershopStylistResponse> CreateStylistAsync(CreateBarbershopStylistRequest request, CancellationToken cancellationToken = default);
    Task<BarbershopStylistResponse> UpdateStylistAsync(Guid id, UpdateBarbershopStylistRequest request, CancellationToken cancellationToken = default);
    Task DeleteStylistAsync(Guid id, CancellationToken cancellationToken = default);

    // --- Portal links ---
    Task<IReadOnlyList<BarbershopPortalLinkResponse>> GetPortalLinksAsync(CancellationToken cancellationToken = default);
    Task<BarbershopPortalLinkResponse> CreatePortalLinkAsync(CreateBarbershopPortalLinkRequest request, CancellationToken cancellationToken = default);
    Task<BarbershopPortalLinkResponse> SetPortalLinkActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task DeletePortalLinkAsync(Guid id, CancellationToken cancellationToken = default);

    // --- Tablero / Citas ---
    Task<IReadOnlyList<BarbershopAppointmentResponse>> GetAppointmentsAsync(DateTime? date, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<BarbershopAppointmentResponse> CreateAppointmentAsync(CreateBarbershopAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<BarbershopAppointmentResponse> AdvanceStatusAsync(Guid id, bool isPaid, CancellationToken cancellationToken = default);
    Task<BarbershopAppointmentResponse> MarkAsPaidAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAppointmentAsync(Guid id, CancellationToken cancellationToken = default);

    // --- Métricas ---
    Task<BarbershopMetricsResponse> GetMetricsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    // --- Disponibilidad de slots ---
    Task<IReadOnlyList<BarbershopSlotResponse>> GetSlotsAsync(DateTime date, int? serviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BarbershopSlotResponse>> GetPublicSlotsAsync(string slug, DateTime date, int? serviceId, CancellationToken cancellationToken = default);

    // --- Portal público ---
    Task<BarbershopPublicLinkResponse> GetPublicLinkAsync(string slug, CancellationToken cancellationToken = default);
    Task<BarbershopBookingResponse> BookBySlugAsync(string slug, BookBarbershopAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<BarbershopAppointmentStatusResponse> GetAppointmentByTokenAsync(string token, CancellationToken cancellationToken = default);
}
