namespace Alkiman.Application.Carwash;

// ============================================================
// Configuración del módulo
// ============================================================

/// <summary>
/// Configuración del negocio. <see cref="OperationMode"/> en <c>null</c>
/// significa que todavía no se eligió modo: el front usa eso para mostrar el
/// diálogo de configuración inicial.
/// </summary>
public record CarwashSettingsResponse(
    string? OperationMode,
    // Ver CarwashTipMode. Con OperationMode en null todavía no hay fila guardada,
    // así que estos dos vienen con el default del dominio.
    string TipMode,
    decimal TipSuggestedPercent);

public record SaveCarwashSettingsRequest(
    string OperationMode,
    string? TipMode,
    decimal? TipSuggestedPercent);

// ============================================================
// Catálogo
// ============================================================

public record CarwashServiceResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int EstimatedMinutes,
    bool IsActive);

public record CreateServiceRequest(string Name, string? Description, decimal Price, int EstimatedMinutes);

public record UpdateServiceRequest(string Name, string? Description, decimal Price, int EstimatedMinutes, bool IsActive);

/// <summary>Agregado opcional del catálogo (encerado, ozono, ...) que se suma al servicio base.</summary>
public record CarwashExtraResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int EstimatedMinutes,
    bool IsActive);

public record CreateExtraRequest(string Name, string? Description, decimal Price, int EstimatedMinutes);

public record UpdateExtraRequest(string Name, string? Description, decimal Price, int EstimatedMinutes, bool IsActive);

public record CarwashPortalLinkResponse(
    Guid Id,
    string Title,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);

public record CreateCarwashPortalLinkRequest(string Title);

// ============================================================
// Cola
// ============================================================

/// <summary>Extra ya aplicado a un ticket: nombre y precio congelados al momento del alta, no los del catálogo actual.</summary>
public record CarwashTicketExtraResponse(int ExtraId, string Name, decimal Price);

/// <summary>
/// Un lavador del directorio del módulo (CWS_Washers): nombre, teléfono y estado.
///
/// No hay campo de cuenta ni de usuario, y es deliberado: la ficha del lavador y
/// la identidad del sistema son cosas separadas (ver <see cref="CarwashWasher"/>).
/// </summary>
public record CarwashWasherResponse(
    Guid Id,
    string FullName,
    string? Phone,
    bool IsActive,
    /// <summary>false si ya tiene tickets: el front ofrece desactivar en vez de eliminar, para no perder el historial.</summary>
    bool CanDelete);

public record CreateWasherRequest(string FullName, string? Phone);

public record UpdateWasherRequest(string FullName, string? Phone, bool IsActive);

public record AssignWasherRequest(Guid? WasherId);

/// <summary>
/// Un ticket de la cola, con los datos del cliente, el servicio y los extras ya
/// embebidos (evita que el front tenga que hacer N+1 llamadas por cada
/// tarjeta del tablero).
/// </summary>
public record CarwashTicketResponse(
    Guid Id,
    int QueueNumber,
    string VehiclePlate,
    string? VehicleBrand,
    string? VehicleModel,
    int? VehicleYear,
    string? VehicleColor,
    Guid CustomerId,
    string CustomerName,
    string? CustomerPhone,
    int ServiceId,
    string ServiceName,
    decimal ServicePrice,
    IReadOnlyList<CarwashTicketExtraResponse> Extras,
    // Total = servicio base + extras, todo a precios congelados.
    decimal Total,
    Guid? AssignedToWasherId,
    string? AssignedToName,
    string Status,
    string Source,
    DateTime? ArrivalDeadline,
    DateTime? ArrivedAt,
    DateTime? StartedAt,
    DateTime? ReadyAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    string? Notes,
    // Propina confirmada al entregar y a quién se le atribuyó. Null mientras el
    // ticket no esté entregado, o si el negocio no maneja propinas.
    decimal? TipAmount,
    Guid? TipWasherId,
    string? TipWasherName,
    DateTime CreatedAt);

/// <summary>Alta presencial de un vehículo por el Encargado: busca-o-crea al cliente por teléfono/email.</summary>
public record RegisterTicketRequest(
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    int ServiceId,
    IReadOnlyList<int>? ExtraIds,
    string VehiclePlate,
    string? VehicleBrand,
    string? VehicleModel,
    int? VehicleYear,
    string? VehicleColor);

/// <summary>
/// Avance de estado. <see cref="TipAmount"/> sólo se mira al pasar a Delivered:
/// es el monto que el mostrador confirma haber recibido, no un cargo. Null o 0
/// significan "sin propina" y son válidos siempre — el sistema no cobra nada.
/// </summary>
public record AdvanceStatusRequest(string Status, decimal? TipAmount);

// ============================================================
// Público (sin login)
// ============================================================

/// <summary>
/// Info pública del link (paso 0 del flujo de auto-registro): catálogo de servicios y extras
/// activos + la apariencia del negocio dueño del link (mismo criterio que <c>PortalCatalog</c>,
/// para que la pantalla pública se pinte con SU marca y no con la de Alkiman).
/// </summary>
public record PublicCarwashLinkResponse(
    string LinkTitle,
    string BusinessName,
    string AppName,
    string ThemeMode,
    string AccentColor,
    IReadOnlyList<CarwashServiceResponse> Services,
    IReadOnlyList<CarwashExtraResponse> Extras);

/// <summary>Auto-registro público: sin LandlordId explícito, se resuelve por el Slug del link.</summary>
public record PublicJoinQueueRequest(
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    int ServiceId,
    IReadOnlyList<int>? ExtraIds,
    string VehiclePlate,
    string? VehicleBrand,
    string? VehicleModel,
    int? VehicleYear,
    string? VehicleColor);

/// <summary>
/// Estado del turno para la página pública (poll-friendly), resuelto por AccessToken.
/// <see cref="AccessToken"/> es lo que el front usa para navegar a <c>/lavado/turno/{token}</c>
/// justo después del alta, así que también se devuelve como respuesta del join.
/// </summary>
public record PublicTicketStatusResponse(
    Guid TicketId,
    Guid AccessToken,
    int QueueNumber,
    string Status,
    DateTime? ArrivalDeadline,
    string VehiclePlate,
    string ServiceName,
    IReadOnlyList<CarwashTicketExtraResponse> Extras,
    decimal Total,
    string BusinessName,
    string AppName,
    string ThemeMode,
    string AccentColor,
    DateTime CreatedAt);
