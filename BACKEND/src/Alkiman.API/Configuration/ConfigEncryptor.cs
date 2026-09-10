using System.Security.Cryptography;
using System.Text;

namespace Alkiman.API.Configuration;

/// <summary>
/// Cifra y descifra valores de configuración con AES-256-GCM.
///
/// Formato del valor cifrado: ENC(base64_nonce:base64_ciphertext:base64_tag)
///
/// La clave de cifrado proviene exclusivamente de la variable de entorno
/// CONFIG_ENCRYPTION_KEY (32 bytes codificados en Base64). Nunca debe aparecer
/// en ningún archivo de configuración ni en el código fuente.
///
/// Uso para encriptar un valor nuevo: ver BACKEND/tools/EncryptConfigTool/
/// </summary>
public static class ConfigEncryptor
{
    private const string Prefix = "ENC(";
    private const string Suffix = ")";

    public static bool IsEncrypted(string? value) =>
        value != null && value.StartsWith(Prefix, StringComparison.Ordinal) && value.EndsWith(Suffix, StringComparison.Ordinal);

    public static string Encrypt(string plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return $"{Prefix}{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(ciphertext)}:{Convert.ToBase64String(tag)}{Suffix}";
    }

    public static string Decrypt(string encryptedValue, byte[] key)
    {
        if (!IsEncrypted(encryptedValue))
            throw new ArgumentException("El valor no tiene formato ENC(...).", nameof(encryptedValue));

        ValidateKey(key);

        var inner = encryptedValue[Prefix.Length..^Suffix.Length];
        var parts = inner.Split(':');

        if (parts.Length != 3)
            throw new FormatException($"Formato de cifrado inválido en valor de configuración. Se esperan 3 segmentos separados por ':'.");

        var nonce = Convert.FromBase64String(parts[0]);
        var ciphertext = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != 32)
            throw new ArgumentException("La clave debe ser exactamente 32 bytes (AES-256).", nameof(key));
    }
}
