using Alkiman.Domain.Entities;

namespace Alkiman.Application.Contracts;

public interface IContractService
{
    /// <summary>
    /// Genera automáticamente el contrato de una renta recién creada: busca la plantilla
    /// activa de la categoría del activo (o usa un texto genérico si el negocio todavía no
    /// configuró ninguna), mergea los datos de la renta/cliente/activo, renderiza el PDF,
    /// lo persiste y se lo envía por correo al cliente. Nunca lanza por una falla de envío
    /// de correo (mismo criterio que EmailService): el contrato queda igual generado y
    /// disponible, con EmailSent = false, para reenviar más adelante.
    /// Se llama tanto desde la creación manual de rentas (RentalService) como desde el
    /// checkout del Portal público (PortalService).
    /// </summary>
    Task<Contract> GenerateForRentalAsync(
        Rental rental,
        Asset asset,
        Customer customer,
        Guid landlordId,
        string createdBy,
        string? signatureImageBase64,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve solo el texto del contrato mergeado (plantilla activa de la categoría del
    /// activo, o el texto genérico si no hay ninguna configurada) para que el cliente lo pueda
    /// leer y aceptar ANTES de firmar y pagar (paso previo a GenerateForRentalAsync en el
    /// Portal). No persiste nada, no renderiza PDF, no envía correo: es puramente de lectura,
    /// pensada para reutilizar exactamente el mismo merge que se usa al generar el contrato
    /// real, de forma que la vista previa coincida con lo que se genera después.
    /// </summary>
    Task<string> PreviewContentAsync(
        Asset asset,
        string customerFullName,
        string? customerIdentityNumber,
        string? customerPhone,
        string? customerEmail,
        string? customerAddress,
        DateTime startDate,
        DateTime endDate,
        decimal totalPrice,
        Guid landlordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ContractResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContractResponse?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default);
    Task<(byte[] Bytes, string FileName)> GetPdfAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Registra la firma del cliente en un contrato pendiente (ej: rentas creadas manualmente) y reenvía el PDF firmado por correo.</summary>
    Task<ContractResponse> SignAsync(Guid id, SignContractRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reenvía por correo el PDF ya generado de un contrato, a demanda (no solo automáticamente al crearse).</summary>
    Task<ContractResponse> ResendEmailAsync(Guid id, CancellationToken cancellationToken = default);
}
