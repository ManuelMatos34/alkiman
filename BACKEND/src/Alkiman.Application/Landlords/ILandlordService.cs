namespace Alkiman.Application.Landlords;

public interface ILandlordService
{
    /// <summary>Perfil del landlord autenticado actualmente.</summary>
    Task<LandlordResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<LandlordResponse> UpdateCurrentAsync(UpdateLandlordRequest request, CancellationToken cancellationToken = default);

    Task<LandlordResponse> UpdateAppearanceAsync(UpdateAppearanceRequest request, CancellationToken cancellationToken = default);

    Task<LandlordResponse> UpdateSignatureAsync(UpdateLandlordSignatureRequest request, CancellationToken cancellationToken = default);
}
