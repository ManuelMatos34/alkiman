namespace Alkiman.Domain.Entities;

/// <summary>Provincia / estado / departamento, dependiente de un país. Tabla: CFG_States.</summary>
public class State
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
