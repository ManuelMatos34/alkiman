using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Link público (sin login) generado por el negocio a partir de un Grupo de
/// Activos (<see cref="AssetGroup"/>) ya existente. Quien recibe el link ve
/// solo los activos de ese grupo y puede auto-rentar uno mediante la pasarela
/// pública del portal. Tabla: PRT_PortalLinks.
/// </summary>
public class PortalLink : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public int AssetGroupId { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
