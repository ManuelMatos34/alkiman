namespace Alkiman.Application.Common.Interfaces;

/// <summary>Hashing y verificación de contraseñas (PBKDF2), sin dependencias externas.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
