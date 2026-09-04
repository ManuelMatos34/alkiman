namespace Alkiman.Application.Contracts;

/// <summary>Datos ya mergeados que necesita el renderizador para producir el PDF de un contrato.</summary>
public record ContractPdfModel(
    string BusinessName,
    string CustomerName,
    string Content,
    DateTime GeneratedAt,
    string? SignatureImageBase64,
    DateTime? SignedAt,
    string? LandlordSignatureBase64);

/// <summary>
/// Puerto de renderizado de PDF. Implementado en Infrastructure con QuestPDF (no hay
/// todavía un editor visual de contratos: el contenido es texto plano con placeholders,
/// ver ContractTemplate.Content) para no sobre-diseñar antes de definir el contenido
/// legal real de los contratos (eso se hará en una iteración futura).
/// </summary>
public interface IContractPdfRenderer
{
    byte[] Render(ContractPdfModel model);
}
