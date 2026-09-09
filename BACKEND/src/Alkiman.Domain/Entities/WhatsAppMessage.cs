namespace Alkiman.Domain.Entities;

public class WhatsAppMessage
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string ToPhone { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
