namespace Alkiman.Application.Portal;

/// <summary>Casos de uso públicos (sin autenticación) del Portal de Rentas: catálogo y checkout.</summary>
public interface IPortalService
{
    Task<PortalCatalogResponse> GetCatalogAsync(string slug, CancellationToken cancellationToken = default);
    Task<PortalCheckoutResponse> CheckoutAsync(string slug, PortalCheckoutRequest request, CancellationToken cancellationToken = default);
    Task<PortalContractPreviewResponse> PreviewContractAsync(string slug, PortalContractPreviewRequest request, CancellationToken cancellationToken = default);
}
