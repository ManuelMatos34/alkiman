namespace Alkiman.Application.PortalLinks;

public interface IPortalLinkService
{
    Task<IReadOnlyList<PortalLinkResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PortalLinkResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortalLinkResponse> CreateAsync(CreatePortalLinkRequest request, CancellationToken cancellationToken = default);
    Task<PortalLinkResponse> UpdateAsync(Guid id, UpdatePortalLinkRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
