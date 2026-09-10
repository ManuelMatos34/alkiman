namespace Alkiman.API.Configuration;

/// <summary>
/// Extensión de arranque que descifra en memoria todos los valores de configuración
/// con formato ENC(...) usando la clave de la variable de entorno CONFIG_ENCRYPTION_KEY.
///
/// Debe llamarse justo después de WebApplication.CreateBuilder(args), antes de registrar
/// cualquier servicio, para que JWT, cadenas de conexión y tokens ya lleguen descifrados
/// a los servicios de infraestructura.
///
/// Si CONFIG_ENCRYPTION_KEY no está definida, los valores quedan tal cual (útil en
/// entornos de desarrollo donde no se usan valores cifrados).
/// </summary>
public static class EncryptedConfigurationExtensions
{
    public static void DecryptEncryptedValues(this ConfigurationManager config)
    {
        var keyBase64 = Environment.GetEnvironmentVariable("CONFIG_ENCRYPTION_KEY");
        if (string.IsNullOrEmpty(keyBase64)) return;

        byte[] key;
        try
        {
            key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "CONFIG_ENCRYPTION_KEY no es un valor Base64 válido. " +
                "Genera una clave con: openssl rand -base64 32");
        }

        if (key.Length != 32)
            throw new InvalidOperationException(
                $"CONFIG_ENCRYPTION_KEY debe ser exactamente 32 bytes (AES-256). " +
                $"La clave proporcionada tiene {key.Length} bytes.");

        var decryptedPairs = new Dictionary<string, string?>();

        foreach (var kvp in config.AsEnumerable())
        {
            if (!ConfigEncryptor.IsEncrypted(kvp.Value)) continue;

            try
            {
                decryptedPairs[kvp.Key] = ConfigEncryptor.Decrypt(kvp.Value!, key);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo descifrar el valor de configuración '{kvp.Key}'. " +
                    $"Verifica que fue cifrado con la misma clave. Detalle: {ex.Message}");
            }
        }

        if (decryptedPairs.Count > 0)
            config.AddInMemoryCollection(decryptedPairs);
    }
}
