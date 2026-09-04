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
}
