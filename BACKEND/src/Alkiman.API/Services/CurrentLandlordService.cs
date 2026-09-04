using System.Security.Claims;
using Alkiman.Application.Common.Interfaces;

namespace Alkiman.API.Services;

/// <summary>
/// Resuelve el aislamiento de datos por negocio (LandlordId) a partir del claim
/// 'landlord_id' del JWT propio de la request actual. El 'sub' del token ya no
/// es el LandlordId: es el UserId del usuario autenticado (ver <see cref="CurrentUserService"/>).
/// </summary>
public class CurrentLandlordService : ICurrentLandlordService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentLandlordService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual.");

    public Task<Guid> GetCurrentLandlordIdAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = _httpContextAccessor.HttpContext?.User.FindFirstValue("landlord_id")
            ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual.");
        return Task.FromResult(Guid.Parse(landlordId));
    }
}
