using System.Security.Cryptography;
using System.Text;

/*
 * EncryptConfigTool — Herramienta de línea de comandos para cifrar valores
 * de configuración con AES-256-GCM (el mismo algoritmo que usa la API).
 *
 * USO:
 *
 *   1. Generar una clave nueva (solo la primera vez):
 *      > dotnet run -- generate-key
 *      Guarda el resultado como variable de entorno CONFIG_ENCRYPTION_KEY en
 *      el servidor y en tu gestor de secretos (nunca en código ni appsettings).
 *
 *   2. Cifrar un valor:
 *      > CONFIG_ENCRYPTION_KEY="<tu-clave-base64>" dotnet run -- encrypt "mi-valor-secreto"
 *      Copia el resultado ENC(...) al appsettings.json correspondiente.
 *
 *   3. Verificar que el descifrado funciona:
 *      > CONFIG_ENCRYPTION_KEY="<tu-clave-base64>" dotnet run -- decrypt "ENC(...)"
 */

if (args.Length == 0)
{
    PrintHelp();
    return 1;
}

return args[0].ToLower() switch
{
    "generate-key" => GenerateKey(),
    "encrypt" when args.Length >= 2 => EncryptValue(args[1]),
    "decrypt" when args.Length >= 2 => DecryptValue(args[1]),
    _ => PrintHelp()
};

static int GenerateKey()
{
    var key = new byte[32];
    RandomNumberGenerator.Fill(key);
    Console.WriteLine("CONFIG_ENCRYPTION_KEY generada (guárdala en un gestor de secretos):");
    Console.WriteLine();
    Console.WriteLine(Convert.ToBase64String(key));
    Console.WriteLine();
    Console.WriteLine("Configura esta variable de entorno en tu servidor antes de arrancar la API.");
    return 0;
}

static int EncryptValue(string plaintext)
{
    var key = LoadKey();
    if (key is null) return 1;

    var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
    RandomNumberGenerator.Fill(nonce);

    var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
    var ciphertext = new byte[plaintextBytes.Length];
    var tag = new byte[AesGcm.TagByteSizes.MaxSize];

    using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
    aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

    var result = $"ENC({Convert.ToBase64String(nonce)}:{Convert.ToBase64String(ciphertext)}:{Convert.ToBase64String(tag)})";
    Console.WriteLine(result);
    return 0;
}

static int DecryptValue(string encryptedValue)
{
    var key = LoadKey();
    if (key is null) return 1;

    if (!encryptedValue.StartsWith("ENC(") || !encryptedValue.EndsWith(")"))
    {
        Console.Error.WriteLine("Error: el valor no tiene el formato ENC(...).");
        return 1;
    }

    var inner = encryptedValue[4..^1];
    var parts = inner.Split(':');

    if (parts.Length != 3)
    {
        Console.Error.WriteLine("Error: formato inválido. Se esperan 3 segmentos separados por ':'.");
        return 1;
    }

    var nonce = Convert.FromBase64String(parts[0]);
    var ciphertext = Convert.FromBase64String(parts[1]);
    var tag = Convert.FromBase64String(parts[2]);
    var plaintext = new byte[ciphertext.Length];

    using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
    aes.Decrypt(nonce, ciphertext, tag, plaintext);

    Console.WriteLine(Encoding.UTF8.GetString(plaintext));
    return 0;
}

static byte[]? LoadKey()
{
    var keyBase64 = Environment.GetEnvironmentVariable("CONFIG_ENCRYPTION_KEY");
    if (string.IsNullOrEmpty(keyBase64))
    {
        Console.Error.WriteLine("Error: la variable de entorno CONFIG_ENCRYPTION_KEY no está definida.");
        return null;
    }

    var key = Convert.FromBase64String(keyBase64);
    if (key.Length != 32)
    {
        Console.Error.WriteLine($"Error: la clave debe ser exactamente 32 bytes. Esta tiene {key.Length} bytes.");
        return null;
    }

    return key;
}

static int PrintHelp()
{
    Console.WriteLine("""
        EncryptConfigTool — Cifrado AES-256-GCM para appsettings

        Comandos:
          generate-key              Genera una nueva clave de 256 bits en Base64
          encrypt <valor>           Cifra un valor con la clave en CONFIG_ENCRYPTION_KEY
          decrypt <valor-ENC>       Descifra un valor ENC(...) para verificación

        Variable de entorno requerida para encrypt/decrypt:
          CONFIG_ENCRYPTION_KEY     Clave AES-256 en Base64 (32 bytes)
        """);
    return 0;
}
