namespace Alkiman.Domain.Entities;

public class LandlordWhatsApp
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string PhoneNumberId { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
