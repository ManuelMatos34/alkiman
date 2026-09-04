namespace Alkiman.Domain.Entities;

/// <summary>Ciudad principal, dependiente de una provincia/estado. Tabla: CFG_Cities.</summary>
public class City
{
    public int Id { get; set; }
    public int StateId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
