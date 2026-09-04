using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alkiman.Application.Emails;
using Microsoft.Extensions.Configuration;

namespace Alkiman.Infrastructure.Email;

/// <summary>
/// Implementación real de <see cref="IEmailSender"/> contra la API REST de Resend
/// (https://resend.com/docs/api-reference/emails/send-email). Igual que con StripeGateway,
/// se llama directo con <see cref="HttpClient"/> (typed client vía AddHttpClient en
/// DependencyInjection) en vez de un SDK completo, ya que es una sola llamada HTTP.
///
/// Nota sobre el modo sandbox de Resend: mientras no se verifique un dominio propio en
/// resend.com/domains, la cuenta solo puede enviar con from=onboarding@resend.dev y
/// únicamente a la casilla con la que te registraste en Resend (a cualquier otro
/// destinatario responde 403 "You can only send testing emails to your own email address").
/// Email:FromAddress/Email:FromName se leen de configuración para poder pasar a un dominio
/// propio verificado sin tocar código.
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private const string BaseUrl = "https://api.resend.com";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(BaseUrl);

        _apiKey = configuration["Email:ResendApiKey"] ?? string.Empty;
        _fromAddress = configuration["Email:FromAddress"] ?? "onboarding@resend.dev";
        _fromName = configuration["Email:FromName"] ?? "Alkiman";
    }

    public Task<EmailSendResult> SendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
        => SendInternalAsync(toEmail, subject, body, attachments: null, cancellationToken);

    public Task<EmailSendResult> SendWithAttachmentsAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default)
        => SendInternalAsync(toEmail, subject, body, attachments, cancellationToken);

    private async Task<EmailSendResult> SendInternalAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new EmailSendResult(false, "No hay una API key de Resend configurada (Email:ResendApiKey).");

        if (string.IsNullOrWhiteSpace(toEmail))
            return new EmailSendResult(false, "El destinatario no tiene un correo válido.");

        var payload = new ResendEmailRequest
        {
            From = $"{_fromName} <{_fromAddress}>",
            To = new[] { toEmail },
            Subject = subject,
            Html = body,
            Attachments = attachments?.Select(a => new ResendAttachment
            {
                Filename = a.FileName,
                Content = Convert.ToBase64String(a.Content)
            }).ToList()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/emails")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = TryExtractErrorMessage(responseBody)
                    ?? $"Resend rechazó el envío ({(int)response.StatusCode}).";
                return new EmailSendResult(false, errorMessage);
            }

            return new EmailSendResult(true, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new EmailSendResult(false, $"No se pudo contactar a Resend: {ex.Message}");
        }
    }

    private static string? TryExtractErrorMessage(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ResendErrorResponse>(responseBody, JsonOptions);
            return error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = default!;

        [JsonPropertyName("to")]
        public string[] To { get; set; } = default!;

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = default!;

        [JsonPropertyName("html")]
        public string Html { get; set; } = default!;

        [JsonPropertyName("attachments")]
        public List<ResendAttachment>? Attachments { get; set; }
    }

    private sealed class ResendAttachment
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = default!;

        /// <summary>Contenido del adjunto en Base64 (Resend lo espera así, sin prefijo data URL).</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = default!;
    }

    private sealed class ResendErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
