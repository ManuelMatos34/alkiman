using Alkiman.Application.Auth;
using Alkiman.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/auth")]
// Casi todo este controller es anónimo, pero el [Authorize] va acá y el [AllowAnonymous]
// en cada acción, no al revés: ASP.NET Core le da prioridad a AllowAnonymous sin importar
// el nivel, así que un [AllowAnonymous] de clase deja sin efecto al [Authorize] de la
// acción y "refresh" quedaba abierto (reventaba en 500 al no encontrar el usuario).
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly ICurrentLandlordService _currentLandlord;

    public AuthController(IAuthService service, ICurrentLandlordService currentLandlord)
    {
        _service = service;
        _currentLandlord = currentLandlord;
    }

    /// <summary>Crea una cuenta (negocio + credenciales) y devuelve el JWT de sesión.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.RegisterAsync(request, cancellationToken));

    /// <summary>
    /// Autentica con email/contraseña. Devuelve el JWT de sesión, salvo que el usuario
    /// tenga el segundo factor activo: en ese caso devuelve un token de desafío y el
    /// código va por correo (ver <see cref="VerifyTwoFactor"/>).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _service.LoginAsync(request, cancellationToken));

    /// <summary>Segundo paso del login con doble factor: canjea el código del correo por el JWT.</summary>
    [HttpPost("two-factor/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
        => Ok(await _service.VerifyTwoFactorAsync(request, cancellationToken));

    /// <summary>Reenvía el código de doble factor para un desafío en curso.</summary>
    [HttpPost("two-factor/resend")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendTwoFactor(ResendTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await _service.ResendTwoFactorAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reemite el JWT del usuario autenticado con su rol y permisos al día, sin pedir
    /// credenciales de nuevo. Se usa después de habilitar un módulo: los permisos son
    /// claims del token, así que sin esto el usuario recibe 403 hasta el próximo login.
    ///
    /// Es el único endpoint de este controller que exige token; el resto es anónimo.
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentLandlord.UserId);
        return Ok(await _service.RefreshAsync(userId, cancellationToken));
    }

    /// <summary>
    /// Pide un link de recuperación de contraseña por email. Siempre responde 200,
    /// exista o no una cuenta con ese email (no se filtra esa información).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _service.ForgotPasswordAsync(request, cancellationToken);
        return Ok();
    }

    /// <summary>Confirma la recuperación de contraseña con el token recibido por email.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _service.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
