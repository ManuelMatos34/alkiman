namespace Alkiman.Application.Common.Interfaces;

/// <summary>Emite el JWT propio de la API para un usuario autenticado.</summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(
        Guid userId,
        Guid landlordId,
        string email,
        string fullName,
        string businessName,
        string roleName,
        bool isOwner,
        IEnumerable<string> permissions);
}
