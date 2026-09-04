using Alkiman.Application.Emails;

namespace Alkiman.Infrastructure.Email;

/// <summary>
/// Implementación "no-op" de <see cref="IEmailSender"/>: no envía nada, siempre
/// devuelve un resultado de fallo con el motivo explícito. Ya no está registrada en
/// Alkiman.Infrastructure.DependencyInjection (se reemplazó por <see cref="ResendEmailSender"/>
/// una vez configurado un proveedor real); se deja la clase como referencia/fallback
/// por si en algún entorno se necesita desactivar el envío real sin borrar código.
/// </summary>
public class NoOpEmailSender : IEmailSender
{
    public Task<EmailSendResult> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new EmailSendResult(false, "No hay un proveedor de correo configurado todavía."));

    public Task<EmailSendResult> SendWithAttachmentsAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new EmailSendResult(false, "No hay un proveedor de correo configurado todavía."));
}
