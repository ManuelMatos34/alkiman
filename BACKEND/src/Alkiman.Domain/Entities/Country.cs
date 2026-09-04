namespace Alkiman.Domain.Entities;

/// <summary>País del catálogo geográfico. Tabla: CFG_Countries.</summary>
public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string IsoCode { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
