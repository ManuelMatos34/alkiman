namespace Alkiman.Application.Barbershop;

// --- Responses ---
public record BarbershopServiceResponse(int Id, string Name, string? Description, decimal Price, int DurationMinutes, bool IsActive);
public record BarbershopStylistResponse(Guid Id, string FullName, string? Phone, string? Email, bool IsActive);
public record BarbershopPortalLinkResponse(Guid Id, Guid? StylistId, string? StylistName, string Title, string Slug, bool IsActive, DateTime CreatedAt);
public record BarbershopAppointmentResponse(
    Guid Id, Guid? StylistId, string? StylistName, int? ServiceId, string? ServiceName, decimal? ServicePrice,
    string ClientName, string? ClientPhone, string? ClientEmail, string? Notes,
    DateTime ScheduledAt, string Source, string Status, bool IsPaid, DateTime CreatedAt);
public record BarbershopMetricsResponse(
    int TotalAppointments, int CompletedAppointments, int CancelledAppointments,
    decimal TotalRevenue, IReadOnlyList<BarbershopDailyPoint> DailyPoints,
    IReadOnlyList<BarbershopServiceUsage> TopServices,
    IReadOnlyList<BarbershopStylistRanking> StylistRanking);
public record BarbershopDailyPoint(DateOnly Date, int Count, decimal Revenue);
public record BarbershopServiceUsage(string ServiceName, int Count);
public record BarbershopStylistRanking(string StylistName, int Count, decimal Revenue);
public record BarbershopPublicLinkResponse(string Title, bool IsActive, Guid? StylistId, string? StylistName,
    IReadOnlyList<BarbershopServiceResponse> Services);
public record BarbershopBookingResponse(Guid AppointmentId, string TrackingToken, DateTime ScheduledAt);
public record BarbershopAppointmentStatusResponse(Guid Id, string ClientName, string? StylistName, string? ServiceName, DateTime ScheduledAt, string Status);

public record BarbershopSlotResponse(DateTime SlotTime, bool IsAvailable);

// --- Requests ---
public record CreateBarbershopServiceRequest(string Name, string? Description, decimal Price, int DurationMinutes);
public record UpdateBarbershopServiceRequest(string Name, string? Description, decimal Price, int DurationMinutes, bool IsActive);
public record CreateBarbershopStylistRequest(string FullName, string? Phone, string? Email);
public record UpdateBarbershopStylistRequest(string FullName, string? Phone, string? Email, bool IsActive);
public record CreateBarbershopPortalLinkRequest(Guid? StylistId, string Title, string Slug);
public record CreateBarbershopAppointmentRequest(Guid? StylistId, int? ServiceId, string ClientName, string? ClientPhone, string? ClientEmail, string? Notes, DateTime ScheduledAt);
public record BookBarbershopAppointmentRequest(string ClientName, string? ClientPhone, string? ClientEmail, string? Notes, int? ServiceId, DateTime ScheduledAt);
public record SetBarbershopPortalLinkActiveRequest(bool IsActive);
public record AdvanceBarbershopStatusRequest(bool IsPaid = false);
