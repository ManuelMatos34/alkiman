using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Negocio (tenant) dado de alta en la plataforma. Ya no guarda credenciales de
/// acceso: eso vive en <see cref="User"/>, ya que un mismo negocio puede tener
/// varios usuarios (multiusuario, con roles y permisos). Tabla: CFG_Landlords.
/// </summary>
public class Landlord : IAuditable
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = default!;

    public string AppName { get; set; } = "Alkiman";
    public string ThemeMode { get; set; } = "light";
    public string AccentColor { get; set; } = "blue";

    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
    public string? Address { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? TaxId { get; set; }
    public string? SignatureBase64 { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
