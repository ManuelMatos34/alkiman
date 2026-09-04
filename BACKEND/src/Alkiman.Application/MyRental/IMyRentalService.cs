namespace Alkiman.Application.MyRental;

/// <summary>Casos de uso públicos (sin autenticación) del link "mi-renta": el cliente entra con el AccessToken de su renta, verifica su identidad y puede pedir una prórroga o una cancelación.</summary>
public interface IMyRentalService
{
    Task<MyRentalResponse> VerifyAsync(Guid token, VerifyMyRentalRequest request, CancellationToken cancellationToken = default);
    Task RequestExtensionAsync(Guid token, CreateMyRentalExtensionRequest request, CancellationToken cancellationToken = default);
    Task RequestCancellationAsync(Guid token, CreateMyRentalCancellationRequest request, CancellationToken cancellationToken = default);
}
