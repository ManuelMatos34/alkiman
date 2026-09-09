using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Alkiman.Application.Common.Interfaces;

namespace Alkiman.API.Services;

/// <summary>
/// Resuelve la identidad completa (usuario, rol, permisos) a partir de los
/// claims del JWT propio de la request actual.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual.");

    public Guid UserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual."));

    public Guid LandlordId =>
        Guid.Parse(User.FindFirstValue("landlord_id")
            ?? throw new InvalidOperationException("No hay un usuario autenticado en el contexto actual."));

    public string Role => User.FindFirstValue("role") ?? "—";

    public bool IsOwner => User.FindFirstValue("is_owner") is "true";

    public IReadOnlyList<string> Permissions =>
        User.FindAll("permission").Select(c => c.Value).ToList();
}
