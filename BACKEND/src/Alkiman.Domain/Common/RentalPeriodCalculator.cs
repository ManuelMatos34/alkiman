using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Common;

/// <summary>
/// Traduce el <see cref="RentalTypeOption"/> de un activo (la unidad de tiempo en la que se
/// renta) a fechas y precios concretos. Es el único lugar que sabe "cuánto dura un período" y
/// "cuánto vale N períodos", para que esa lógica no se duplique entre el checkout del Portal y
/// cualquier otro flujo que la necesite en el futuro.
/// </summary>
public static class RentalPeriodCalculator
{
    /// <summary>Cantidad mínima de períodos que se le puede vender a un cliente (1 = no se puede rentar menos que la unidad del activo).</summary>
    public const int MinimumPeriods = 1;

    /// <summary>Calcula la fecha de fin sumándole <paramref name="periods"/> unidades de <paramref name="rentalType"/> a <paramref name="startDate"/>.</summary>
    public static DateTime AddPeriods(DateTime startDate, RentalTypeOption rentalType, int periods)
    {
        if (periods < MinimumPeriods)
            throw new ArgumentOutOfRangeException(nameof(periods), $"La cantidad de períodos debe ser al menos {MinimumPeriods}.");

        return rentalType switch
        {
            RentalTypeOption.Daily => startDate.AddDays(periods),
            RentalTypeOption.Weekly => startDate.AddDays(periods * 7),
            RentalTypeOption.Biweekly => startDate.AddDays(periods * 15),
            RentalTypeOption.Monthly => startDate.AddMonths(periods),
            RentalTypeOption.Annual => startDate.AddYears(periods),
            _ => throw new ArgumentOutOfRangeException(nameof(rentalType), rentalType, "Tipo de renta no soportado.")
        };
    }

    /// <summary>Precio total = precio base del activo × cantidad de períodos × cantidad de unidades rentadas.</summary>
    public static decimal CalculateTotalPrice(decimal basePrice, int periods, int quantity)
    {
        if (periods < MinimumPeriods)
            throw new ArgumentOutOfRangeException(nameof(periods), $"La cantidad de períodos debe ser al menos {MinimumPeriods}.");
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser al menos 1.");

        return basePrice * periods * quantity;
    }
}
