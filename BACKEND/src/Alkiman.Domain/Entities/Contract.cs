using Alkiman.Domain.Common;
using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Contrato generado automáticamente al crear una renta (manual o desde el Portal),
/// a partir de la <see cref="ContractTemplate"/> activa de la categoría del activo
/// rentado (o de una plantilla genérica si el negocio todavía no configuró ninguna).
/// Guarda una foto del contenido usado (ContentSnapshot) y el PDF ya renderizado
/// (PdfContent) para que ediciones futuras de la plantilla no alteren contratos ya
/// emitidos. Tabla: COM_Contracts.
/// </summary>
public class Contract : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid RentalId { get; set; }
    public Guid CustomerId { get; set; }
    /// <summary>Plantilla usada para generarlo. Null si se usó el texto genérico por defecto (todavía no hay plantilla configurada para la categoría).</summary>
    public int? ContractTemplateId { get; set; }
    /// <summary>Texto final ya con los placeholders reemplazados, tal como se usó para renderizar el PDF.</summary>
    public string ContentSnapshot { get; set; } = default!;
    /// <summary>PDF ya renderizado (QuestPDF), se guarda en la fila porque todavía no hay blob storage configurado.</summary>
    public byte[] PdfContent { get; set; } = default!;
    /// <summary>Firma del cliente como imagen PNG codificada en Base64 (data URL sin el prefijo "data:image/png;base64,"). Null hasta que se firma.</summary>
    public string? SignatureImageBase64 { get; set; }
    public ContractStatus Status { get; set; }
    public DateTime? SignedAt { get; set; }
    public bool EmailSent { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
