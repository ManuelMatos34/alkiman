using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Alkiman.Application.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Alkiman.API.Authentication;

/// <summary>
/// Confirma contra la base que el usuario detrás de un JWT válido sigue existiendo y
/// sigue habilitado, en cada request.
///
/// Sin esto, la firma y el vencimiento son lo único que se mira: un usuario eliminado
/// o desactivado conserva acceso completo hasta que su token caduque (8 h por defecto).
/// Es decir, el switch "Usuario activo" del panel no tenía efecto inmediato, que es
/// justo lo que un admin espera cuando desactiva a alguien que se fue del negocio.
///
/// Además evita un modo de falla feo: si el usuario o su negocio ya no están, las
/// escrituras reventaban recién en la base, con un choque de foreign key que salía como
/// 500 en vez de un 401 que explique que la sesión ya no vale.
///
/// El costo es una búsqueda por clave primaria por request autenticado. Es el precio de
/// que la revocación sea inmediata; si algún día pesa, se cachea con TTL corto acá mismo,
/// sin tocar el resto de la aplicación.
/// </summary>
public static class ActiveUserJwtEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var subject = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(subject, out var userId))
            {
                context.Fail("El token no identifica a un usuario válido.");
                return;
            }

            var repository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = await repository.GetByIdAsync(userId, context.HttpContext.RequestAborted);

            if (user is null)
            {
                context.Fail("La cuenta asociada a esta sesión ya no existe.");
                return;
            }

            if (!user.IsActive)
            {
                context.Fail("La cuenta asociada a esta sesión está desactivada.");
                return;
            }

            // El negocio viaja en el token pero manda el de la base: si no coinciden, el
            // token quedó viejo o fue manipulado, y dejarlo pasar significaría escribir
            // filas en el tenant equivocado.
            var landlordClaim = principal?.FindFirst("landlord_id")?.Value;
            if (!Guid.TryParse(landlordClaim, out var landlordId) || landlordId != user.LandlordId)
            {
                context.Fail("La sesión no corresponde al negocio del usuario.");
                return;
            }
        },
    };
}
