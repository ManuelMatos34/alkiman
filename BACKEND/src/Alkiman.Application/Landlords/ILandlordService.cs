namespace Alkiman.Application.Landlords;

public interface ILandlordService
{
    /// <summary>Perfil del landlord autenticado actualmente.</summary>
    Task<LandlordResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Completa el registro del negocio para el usuario de Auth0 autenticado (primer login).</summary>
    Task<LandlordResponse> RegisterAsync(RegisterLandlordRequest request, CancellationToken cancellationToken = default);

    Task<LandlordResponse> UpdateCurrentAsync(UpdateLandlordRequest request, CancellationToken cancellationToken = default);
}
