namespace Alkiman.Domain.Enums;

/// <summary>
/// Control maestro de negocio de un activo (columna INV_Assets.RentalType): define la
/// unidad de tiempo en la que se renta el activo (ej: un apartamento se renta por Monthly,
/// una herramienta por Daily). Esta unidad determina tanto el mínimo de tiempo que un
/// cliente puede rentar en el Portal público (no puede rentar menos de 1 período) como el
/// cálculo del precio total (ver <see cref="Alkiman.Domain.Common.RentalPeriodCalculator"/>).
/// </summary>
public enum RentalTypeOption
{
    Daily,
    Weekly,
    Biweekly,
    Monthly,
    Annual
}
