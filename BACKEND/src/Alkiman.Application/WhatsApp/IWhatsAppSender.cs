namespace Alkiman.Application.WhatsApp;

/// <summary>
/// Envía mensajes de WhatsApp a través de la API Cloud de Meta (WhatsApp Business Platform).
/// Solo soporta mensajes de plantilla aprobados por Meta, que son los únicos permitidos
/// para mensajes proactivos (business-initiated) fuera de la ventana de 24h del cliente.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>
    /// Envía un mensaje de plantilla aprobada por Meta.
    /// </summary>
    /// <param name="toPhone">Número en cualquier formato; se normaliza internamente a E.164 sin '+'.</param>
    /// <param name="templateName">Nombre exacto de la plantilla registrada en Meta Business Manager.</param>
    /// <param name="languageCode">Código de idioma de la plantilla (ej: "es", "es_MX", "en_US").</param>
    /// <param name="bodyParameters">Valores para los parámetros {{1}}, {{2}}, etc. del body de la plantilla.</param>
    Task<WhatsAppSendResult> SendTemplateAsync(
        string toPhone,
        string templateName,
        string languageCode,
        IEnumerable<string> bodyParameters,
        CancellationToken ct = default);
}

public record WhatsAppSendResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Nombres de las plantillas de WhatsApp registradas en Meta Business Manager.
/// Cada constante corresponde a una plantilla que debe existir en el panel de Meta con ese nombre exacto.
///
/// Guía rápida para registrar una plantilla:
///   1. Ir a business.facebook.com → WhatsApp → Plantillas de mensajes
///   2. Crear plantilla con categoría "Utility" (más barata que Marketing)
///   3. Usar los {{1}}, {{2}}, etc. como variables de cuerpo
///   4. Esperar aprobación (generalmente minutos para Utility)
/// </summary>
public static class WhatsAppTemplates
{
    /// <summary>
    /// Plantilla: cambio de estado de turno en carwash.
    /// Parámetros: {{1}} nombre, {{2}} placa, {{3}} estado
    /// Ejemplo body: "Hola {{1}}, el estado de tu vehículo {{2}} cambió a: *{{3}}*."
    /// </summary>
    public const string CarwashStatus = "hello_world"; // TEMPORAL: usar mientras alkiman_cws_status está en revisión

    /// <summary>
    /// Plantilla: cambio de estado con límite de tiempo (ArrivalPending).
    /// Parámetros: {{1}} nombre, {{2}} placa, {{3}} estado, {{4}} hora límite
    /// Ejemplo body: "Hola {{1}}, tu vehículo {{2}} cambió a: *{{3}}*. Tienes hasta las {{4}} para llegar."
    /// </summary>
    public const string CarwashStatusWithDeadline = "alkiman_carwash_status_deadline";

    /// <summary>
    /// Plantilla: notificación de propina al lavador.
    /// Parámetros: {{1}} nombre lavador, {{2}} monto propina, {{3}} número de turno, {{4}} total del día
    /// Ejemplo body: "💸 ¡Hola {{1}}! Recibiste una propina de {{2}} en el turno #{{3}}. Tu total hoy: {{4}}."
    /// </summary>
    public const string WasherTip = "alkiman_washer_tip";

    /// <summary>
    /// Plantilla: confirmación de cita en barbería.
    /// Parámetros: {{1}} nombre cliente, {{2}} negocio, {{3}} fecha, {{4}} hora, {{5}} servicio
    /// Ejemplo body: "✅ Hola {{1}}, tu cita en {{2}} está confirmada para el {{3}} a las {{4}}. Servicio: {{5}}."
    /// </summary>
    public const string BarbershopBooking = "alkiman_barbershop_booking";

    /// <summary>
    /// Plantilla: resultado de pedido de prórroga o cancelación de renta.
    /// Parámetros: {{1}} nombre, {{2}} tipo pedido (prórroga/cancelación), {{3}} resultado (aprobado/rechazado)
    /// Ejemplo body: "Hola {{1}}, tu pedido de {{2}} fue {{3}}."
    /// </summary>
    public const string RentalDecision = "alkiman_rental_decision";
}
