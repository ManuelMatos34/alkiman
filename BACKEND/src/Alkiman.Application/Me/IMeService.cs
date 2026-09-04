namespace Alkiman.Application.Me;

/// <summary>Operaciones de autoservicio sobre la propia cuenta del usuario autenticado.</summary>
public interface IMeService
{
    Task<MeResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<MeResponse> UpdateAsync(UpdateMeRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangeMyPasswordRequest request, CancellationToken cancellationToken = default);
    Task<MeResponse> UpdateTwoFactorAsync(UpdateMyTwoFactorRequest request, CancellationToken cancellationToken = default);
}
