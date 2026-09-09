using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.WhatsApp;
using Alkiman.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Alkiman.Infrastructure.WhatsApp;

/// <summary>
/// Implementación de IWhatsAppSender usando la API Cloud de Meta (WhatsApp Business Platform).
/// Documentación: https://developers.facebook.com/docs/whatsapp/cloud-api/messages/template-messages
///
/// Configuración requerida en appsettings / User Secrets:
///   WhatsApp:PhoneNumberId  → ID del número de teléfono en Meta (no el número en sí)
///   WhatsApp:AccessToken    → Token de acceso permanente de la app de Meta
///
/// Costo: gratis hasta 1,000 conversaciones/mes; después ~$0.02 USD por conversación de utilidad.
/// </summary>
public class MetaWhatsAppSender : IWhatsAppSender
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly HttpClient _http;
    private readonly ILogger<MetaWhatsAppSender> _logger;
    private readonly string? _phoneNumberId;
    private readonly string? _accessToken;
    private readonly string _apiVersion;
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly ICurrentLandlordService _currentLandlord;

    public MetaWhatsAppSender(
        HttpClient http,
        IConfiguration configuration,
        ILogger<MetaWhatsAppSender> logger,
        IWhatsAppMessageRepository messageRepository,
        ICurrentLandlordService currentLandlord)
    {
        _http = http;
        _logger = logger;
        _phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
        _accessToken   = configuration["WhatsApp:AccessToken"];
        _apiVersion    = configuration["WhatsApp:ApiVersion"] ?? "v20.0";
        _messageRepository = messageRepository;
        _currentLandlord   = currentLandlord;

        _http.BaseAddress = new Uri("https://graph.facebook.com/");
    }

    public async Task<WhatsAppSendResult> SendTemplateAsync(
        string toPhone,
        string templateName,
        string languageCode,
        IEnumerable<string> bodyParameters,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_phoneNumberId) || string.IsNullOrWhiteSpace(_accessToken))
            return new WhatsAppSendResult(false, "WhatsApp no está configurado (PhoneNumberId o AccessToken vacío).");

        var normalizedPhone = NormalizePhone(toPhone);
        if (normalizedPhone is null)
            return new WhatsAppSendResult(false, $"Número de teléfono inválido: {toPhone}");

        var parameters = bodyParameters
            .Select(v => new { type = "text", text = v })
            .ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = normalizedPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = parameters.Length == 0
                    ? Array.Empty<object>()
                    : new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters
                        }
                    }
            }
        };

        var json    = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiVersion}/{_phoneNumberId}/messages");
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Content = content;

        WhatsAppSendResult result;
        try
        {
            var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                result = new WhatsAppSendResult(true);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("WhatsApp API error {Status} para plantilla {Template}: {Body}",
                    (int)response.StatusCode, templateName, errorBody);
                result = new WhatsAppSendResult(false, TryExtractErrorMessage(errorBody));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción enviando WhatsApp a {Phone} con plantilla {Template}", normalizedPhone, templateName);
            result = new WhatsAppSendResult(false, ex.Message);
        }

        // Persistir el registro de envío de forma best-effort (no interrumpe el flujo si falla).
        try
        {
            var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(ct);
            var record = new WhatsAppMessage
            {
                LandlordId   = landlordId,
                ToPhone      = normalizedPhone,
                TemplateName = templateName,
                Status       = result.Success ? "Sent" : "Failed",
                ErrorMessage = result.ErrorMessage,
                SentAt       = result.Success ? DateTime.UtcNow : null,
                CreatedAt    = DateTime.UtcNow,
            };
            await _messageRepository.CreateAsync(record, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir el registro de WhatsApp para plantilla {Template}", templateName);
        }

        return result;
    }

    /// <summary>
    /// Normaliza el número al formato E.164 sin '+' requerido por Meta.
    /// Detecta automáticamente números dominicanos (809/829/849) de 10 dígitos y les agrega el prefijo país 1.
    /// </summary>
    private static string? NormalizePhone(string phone)
    {
        // Quitar todo menos dígitos
        var digits = Regex.Replace(phone, @"\D", "");

        if (digits.Length == 0) return null;

        // 10 dígitos: puede ser número dominicano (809/829/849) u otro número local
        if (digits.Length == 10 && (digits.StartsWith("809") || digits.StartsWith("829") || digits.StartsWith("849")))
            return "1" + digits;   // → 18091234567

        // 11 dígitos empezando con 1: ya tiene prefijo NANP (USA/RD/etc.)
        if (digits.Length == 11 && digits.StartsWith("1"))
            return digits;

        // Cualquier otro caso: devolver tal cual (números internacionales con país)
        if (digits.Length >= 7)
            return digits;

        return null;
    }

    private static string TryExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? json;
            }
        }
        catch { /* ignorar errores de parseo */ }
        return json.Length > 300 ? json[..300] : json;
    }
}
