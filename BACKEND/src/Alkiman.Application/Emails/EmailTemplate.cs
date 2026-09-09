namespace Alkiman.Application.Emails;

/// <summary>
/// Genera HTML de emails transaccionales con diseño consistente.
/// Compatible con los principales clientes de correo (table-based layout, sin CSS externo).
/// </summary>
public static class EmailTemplate
{
    private const string AccentColor  = "#7c3aed";
    private const string AccentLight  = "#ede9fe";

    /// <summary>
    /// Construye el HTML completo de un email transaccional.
    /// </summary>
    /// <param name="title">Subtítulo mostrado en la cabecera (ej: "Verificación de identidad").</param>
    /// <param name="greeting">Primera línea del cuerpo (ej: "Hola Juan,").</param>
    /// <param name="paragraphs">Párrafos de contenido en orden. Se permiten etiquetas HTML básicas (strong, br).</param>
    /// <param name="ctaLabel">Texto del botón de acción principal (opcional).</param>
    /// <param name="ctaUrl">URL del botón de acción principal (opcional).</param>
    /// <param name="highlightCode">Valor destacado en caja grande, ej: código 2FA (opcional).</param>
    /// <param name="highlightLabel">Etiqueta del valor destacado (opcional). Por defecto "Código".</param>
    /// <param name="footerNote">Nota adicional en el pie del correo (opcional).</param>
    /// <param name="businessName">Nombre del negocio mostrado en cabecera y pie. Por defecto "Alkiman".</param>
    public static string Build(
        string title,
        string greeting,
        IEnumerable<string> paragraphs,
        string? ctaLabel     = null,
        string? ctaUrl       = null,
        string? highlightCode  = null,
        string? highlightLabel = null,
        string? footerNote   = null,
        string? businessName = null)
    {
        var business = businessName ?? "Alkiman";

        var paragraphsHtml = string.Concat(paragraphs.Select(p =>
            $"""<p style="margin:0 0 14px;font-size:15px;color:#374151;line-height:1.6;">{p}</p>"""));

        var highlightHtml = string.Empty;
        if (highlightCode is not null)
        {
            var label = highlightLabel ?? "Código";
            highlightHtml = $"""
                <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f3ff;border-radius:8px;margin:4px 0 20px;">
                  <tr>
                    <td style="padding:20px;text-align:center;">
                      <p style="margin:0 0 6px;font-size:12px;font-weight:600;color:#7c3aed;text-transform:uppercase;letter-spacing:.06em;">{label}</p>
                      <p style="margin:0;font-size:34px;font-weight:700;color:#4c1d95;letter-spacing:.14em;font-family:monospace,monospace;">{highlightCode}</p>
                    </td>
                  </tr>
                </table>
                """;
        }

        var ctaHtml = string.Empty;
        if (ctaLabel is not null && ctaUrl is not null)
        {
            ctaHtml = $"""
                <table width="100%" cellpadding="0" cellspacing="0" style="margin:24px 0 8px;">
                  <tr>
                    <td align="center">
                      <a href="{ctaUrl}"
                         style="display:inline-block;padding:13px 32px;background:{AccentColor};color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:8px;">
                        {ctaLabel}
                      </a>
                    </td>
                  </tr>
                </table>
                """;
        }

        var footerNoteHtml = footerNote is not null
            ? $"""<p style="margin:6px 0 0;font-size:12px;color:#9ca3af;">{footerNote}</p>"""
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
            </head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:system-ui,-apple-system,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 16px;">
                <tr><td align="center">
                  <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                    <!-- Cabecera -->
                    <tr>
                      <td style="background:{AccentColor};padding:28px 32px;text-align:center;">
                        <p style="margin:0;font-size:22px;font-weight:700;color:#ffffff;">{business}</p>
                        <p style="margin:6px 0 0;font-size:14px;color:{AccentLight};">{title}</p>
                      </td>
                    </tr>

                    <!-- Cuerpo -->
                    <tr>
                      <td style="padding:28px 32px 20px;">
                        <p style="margin:0 0 18px;font-size:15px;font-weight:600;color:#111827;">{greeting}</p>
                        {paragraphsHtml}
                        {highlightHtml}
                        {ctaHtml}
                      </td>
                    </tr>

                    <!-- Pie -->
                    <tr>
                      <td style="background:#f9fafb;padding:18px 32px;text-align:center;border-top:1px solid #e5e7eb;">
                        <p style="margin:0;font-size:12px;color:#9ca3af;">Notificación automática de {business}</p>
                        {footerNoteHtml}
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
