using Alkiman.Domain.Entities;

namespace Alkiman.Application.Users;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Prende/apaga la exigencia de cambio de contraseña en el próximo login.</summary>
    Task SetMustChangePasswordAsync(Guid id, bool value, CancellationToken cancellationToken = default);

    /// <summary>Guarda (o limpia, pasando null) el token de recuperación de contraseña y su vencimiento.</summary>
    Task SetResetTokenAsync(Guid id, string? token, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Busca el usuario dueño de un token de recuperación vigente o vencido (la validación de vencimiento la hace el servicio).</summary>
    Task<User?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el desafío de segundo factor en curso, o lo borra pasando todo en null
    /// (que es lo que se hace al verificarlo bien, al agotar los intentos y al vencer).
    /// Pisa cualquier desafío anterior: sólo puede haber uno vivo por usuario.
    /// </summary>
    Task SetTwoFactorChallengeAsync(
        Guid id,
        string? challengeToken,
        string? codeHash,
        DateTime? expiresAtUtc,
        int attempts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca al usuario dueño de un token de desafío. Devuelve null si el token no
    /// existe; el vencimiento y el conteo de intentos los evalúa el servicio.
    /// </summary>
    Task<User?> GetByTwoFactorChallengeTokenAsync(string challengeToken, CancellationToken cancellationToken = default);
}
