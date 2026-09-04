using Alkiman.Application.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Alkiman.Infrastructure.Pdf;

/// <summary>
/// Renderiza el PDF de un contrato con QuestPDF (licencia Community) a partir del texto
/// ya mergeado (ContractTemplate.Content con los placeholders reemplazados). No hay
/// todavía un editor visual/rico de contratos: el contenido es texto plano por diseño,
/// ver la nota en IContractPdfRenderer.
/// </summary>
public class QuestPdfContractRenderer : IContractPdfRenderer
{
    public byte[] Render(ContractPdfModel model)
    {
        static byte[]? TryDecode(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                // Firma inválida/corrupta: se ignora la imagen, el contrato queda igual generado.
                return null;
            }
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(model.BusinessName).SemiBold().FontSize(16);
                    column.Item().Text($"Contrato generado el {model.GeneratedAt:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(8);
                    foreach (var paragraph in model.Content.Split('\n'))
                    {
                        column.Item().Text(paragraph.TrimEnd('\r'));
                    }

                    column.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(landlordSignatureColumn =>
                        {
                            landlordSignatureColumn.Item().Text("Firma de la empresa:").SemiBold();

                            var landlordSignatureBytes = TryDecode(model.LandlordSignatureBase64);
                            if (landlordSignatureBytes is not null)
                            {
                                landlordSignatureColumn.Item().PaddingTop(5).MaxHeight(80).Image(landlordSignatureBytes).FitArea();
                            }
                            else
                            {
                                landlordSignatureColumn.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                landlordSignatureColumn.Item().PaddingTop(3).Text("Pendiente de firma").FontSize(9).FontColor(Colors.Grey.Medium);
                            }
                        });

                        row.ConstantItem(20);

                        row.RelativeItem().Column(signatureColumn =>
                        {
                            signatureColumn.Item().Text("Firma del cliente:").SemiBold();

                            if (!string.IsNullOrWhiteSpace(model.SignatureImageBase64))
                            {
                                var signatureBytes = TryDecode(model.SignatureImageBase64);
                                if (signatureBytes is not null)
                                {
                                    signatureColumn.Item().PaddingTop(5).MaxHeight(80).Image(signatureBytes).FitArea();
                                }

                                signatureColumn.Item().PaddingTop(5).Text(
                                    $"Firmado el {model.SignedAt:dd/MM/yyyy HH:mm} por {model.CustomerName}."
                                ).FontSize(9).FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                signatureColumn.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                signatureColumn.Item().PaddingTop(3).Text("Pendiente de firma").FontSize(9).FontColor(Colors.Grey.Medium);
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
