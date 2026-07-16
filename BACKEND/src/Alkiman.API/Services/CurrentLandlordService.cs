using System.Security.Claims;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Landlords;
using Alkiman.Domain.Entities;

namespace Alkiman.API.Services;

/// <summary>
/// Resuelve la identidad del landlord autenticado a partir del claim 'sub'
/// emitido por Auth0 en el token JWT de la request actual.
/// </summary>
public class CurrentLandlordService : ICurrentLandlordService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILandlordRepository _landlordRepository;

    public CurrentLandlordService(IHttpContextAccessor httpContextAccessor, ILandlordRepository landlordRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _landlordRepository = landlordRepository;
    }

    public string Auth0UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual.");

    public async Task<Guid> GetCurrentLandlordIdAsync(CancellationToken cancellationToken = default)
    {
        var landlord = await _landlordRepository.GetByAuth0UserIdAsync(Auth0UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Landlord), Auth0UserId);

        return landlord.Id;
    }
}
