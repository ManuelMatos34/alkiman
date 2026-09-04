namespace Alkiman.Application.Emails;

/// <summary>Resultado real de un intento de envío de correo.</summary>
public record EmailSendResult(bool Success, string? ErrorMessage);

/// <summary>Adjunto binario de un correo (ej: el PDF de un contrato).</summary>
public record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Puerto de envío de correo. Implementación real: <c>ResendEmailSender</c> (Infrastructure),
/// contra la API de Resend (ver Email:ResendApiKey/FromAddress/FromName en configuración).
/// Si no hay API key configurada, devuelve un resultado de fallo con el motivo explícito,
/// para que la pantalla de Correos siga siendo utilizable (historial, destinatarios,
/// recordatorios) aunque el envío real todavía no esté configurado en ese entorno.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Igual que <see cref="SendAsync"/> pero con adjuntos. Se usa para enviarle al
    /// cliente el PDF del contrato apenas se crea su renta (ver ContractService).
    /// </summary>
    Task<EmailSendResult> SendWithAttachmentsAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default
    );
}
