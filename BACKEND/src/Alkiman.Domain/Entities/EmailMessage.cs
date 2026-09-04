using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Registro histórico de un correo (individual o masivo) enviado a un cliente.
/// El envío real depende de un proveedor todavía no configurado (ver IEmailSender);
/// mientras tanto, Status/ErrorMessage reflejan el resultado real del intento.
/// Tabla: COM_EmailMessages.
/// </summary>
public class EmailMessage : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Type { get; set; } = default!; // "Individual" | "Mass"
    public string RecipientName { get; set; } = default!;
    public string RecipientEmail { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Status { get; set; } = default!; // "Sent" | "Failed"
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
