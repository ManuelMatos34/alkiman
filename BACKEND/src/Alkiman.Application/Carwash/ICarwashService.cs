namespace Alkiman.Application.Carwash;

public interface ICarwashService
{
    // Configuración del módulo
    /// <summary>Devuelve OperationMode = null si el negocio todavía no eligió modo (dispara el diálogo inicial en el front).</summary>
    Task<CarwashSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);
    /// <summary>Guarda el modo de operación y, en modo Empresa, siembra el rol de sistema "Lavador" si aún no existe.</summary>
    Task<CarwashSettingsResponse> SaveSettingsAsync(SaveCarwashSettingsRequest request, CancellationToken cancellationToken = default);

    // Catálogo de servicios
    Task<IReadOnlyList<CarwashServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<CarwashServiceResponse> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<CarwashServiceResponse> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default);

    // Catálogo de extras (agregados que se suman al servicio base)
    Task<IReadOnlyList<CarwashExtraResponse>> GetExtrasAsync(CancellationToken cancellationToken = default);
    Task<CarwashExtraResponse> CreateExtraAsync(CreateExtraRequest request, CancellationToken cancellationToken = default);
    Task<CarwashExtraResponse> UpdateExtraAsync(int id, UpdateExtraRequest request, CancellationToken cancellationToken = default);
    Task DeleteExtraAsync(int id, CancellationToken cancellationToken = default);

    // Links de portal
    Task<IReadOnlyList<CarwashPortalLinkResponse>> GetPortalLinksAsync(CancellationToken cancellationToken = default);
    Task<CarwashPortalLinkResponse> CreatePortalLinkAsync(CreateCarwashPortalLinkRequest request, CancellationToken cancellationToken = default);
    Task<CarwashPortalLinkResponse> SetPortalLinkActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    // Directorio de lavadores (entidad propia del módulo, ver CarwashWasher)
    /// <summary>Todo el plantel, activos e inactivos: la pantalla de administración los muestra a todos.</summary>
    Task<IReadOnlyList<CarwashWasherResponse>> GetWashersAsync(CancellationToken cancellationToken = default);
    Task<CarwashWasherResponse> CreateWasherAsync(CreateWasherRequest request, CancellationToken cancellationToken = default);
    Task<CarwashWasherResponse> UpdateWasherAsync(Guid id, UpdateWasherRequest request, CancellationToken cancellationToken = default);
    /// <summary>Solo si nunca lavó nada; si tiene tickets hay que desactivarlo para no perder el historial.</summary>
    Task DeleteWasherAsync(Guid id, CancellationToken cancellationToken = default);

    // Cola (autenticado)
    Task<IReadOnlyList<CarwashTicketResponse>> GetQueueAsync(CancellationToken cancellationToken = default);
    Task<CarwashTicketResponse> RegisterTicketAsync(RegisterTicketRequest request, CancellationToken cancellationToken = default);
    /// <summary>Asigna (o desasigna, pasando WasherId null) el lavador de un ticket. Solo tiene sentido en modo Empresa.</summary>
    Task<CarwashTicketResponse> AssignWasherAsync(Guid ticketId, AssignWasherRequest request, CancellationToken cancellationToken = default);
    Task<CarwashTicketResponse> AdvanceStatusAsync(Guid ticketId, AdvanceStatusRequest request, CancellationToken cancellationToken = default);
    Task<CarwashTicketResponse> CancelTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<CarwashTicketResponse> MarkExpiredAsync(Guid ticketId, CancellationToken cancellationToken = default);
    /// <summary>"Llama" un turno de portal: Waiting -> ArrivalPending con ArrivalDeadline = ahora + deadlineMinutes (default 15).</summary>
    Task<CarwashTicketResponse> CallArrivalAsync(Guid ticketId, int? deadlineMinutes, CancellationToken cancellationToken = default);
    /// <summary>Confirma que el cliente llegó: ArrivalPending -> Waiting, setea ArrivedAt.</summary>
    Task<CarwashTicketResponse> ConfirmArrivalAsync(Guid ticketId, CancellationToken cancellationToken = default);

    // Público (sin login, resuelto por Slug/AccessToken — nunca acepta LandlordId del caller)
    Task<PublicCarwashLinkResponse> GetPublicLinkAsync(string slug, CancellationToken cancellationToken = default);
    Task<PublicTicketStatusResponse> JoinQueueBySlugAsync(string slug, PublicJoinQueueRequest request, CancellationToken cancellationToken = default);
    Task<PublicTicketStatusResponse> GetTicketByTokenAsync(Guid accessToken, CancellationToken cancellationToken = default);
}
