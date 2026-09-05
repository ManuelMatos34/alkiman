using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;

namespace Alkiman.Application.Carwash;

/// <summary>
/// Arma el tablero de Carwash a partir de las agregaciones del repositorio.
/// Acá vive todo lo que es aritmética de presentación (promedios, porcentajes,
/// relleno de días sin movimiento); el SQL sólo cuenta y suma.
/// </summary>
public class CarwashMetricsService : ICarwashMetricsService
{
    private const int DefaultWindowDays = 30;
    private const int MaxWindowDays = 366;
    private const int TopRankingSize = 8;

    private readonly ICarwashMetricsRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public CarwashMetricsService(ICarwashMetricsRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<CarwashMetricsResponse> GetMetricsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var (fromDate, toDate) = ResolveRange(from, to);

        // El repositorio trabaja con un rango semiabierto [fromDate, toExclusive).
        var toExclusive = toDate.AddDays(1);

        var volume = await _repository.GetVolumeTotalsAsync(landlordId, fromDate, toExclusive, cancellationToken);
        var revenue = await _repository.GetRevenueTotalsAsync(landlordId, fromDate, toExclusive, cancellationToken);
        var daily = await _repository.GetDailyVolumeAsync(landlordId, fromDate, toExclusive, cancellationToken);
        var services = await _repository.GetTopServicesAsync(landlordId, fromDate, toExclusive, TopRankingSize, cancellationToken);
        var extras = await _repository.GetTopExtrasAsync(landlordId, fromDate, toExclusive, TopRankingSize, cancellationToken);
        var hourly = await _repository.GetHourlyDistributionAsync(landlordId, fromDate, toExclusive, cancellationToken);
        var washers = await _repository.GetWasherRankingAsync(landlordId, fromDate, toExclusive, cancellationToken);

        var finished = volume.Washed + volume.Cancelled + volume.Expired;
        var volumeDto = new CarwashVolumeSummaryDto(
            volume.Washed,
            volume.Cancelled,
            volume.Expired,
            volume.StillInQueue,
            finished > 0 ? Round((decimal)(volume.Cancelled + volume.Expired) / finished * 100m, 1) : 0m,
            ToWholeMinutes(volume.AverageServiceMinutes)
        );

        var totalRevenue = revenue.ServicesRevenue + revenue.ExtrasRevenue;
        var revenueDto = new CarwashRevenueSummaryDto(
            revenue.ServicesRevenue,
            revenue.ExtrasRevenue,
            totalRevenue,
            // Sobre lavados y no sobre tickets creados: dividir por los cancelados
            // hundiría el ticket promedio sin que nadie haya bajado un precio.
            volume.Washed > 0 ? Round(totalRevenue / volume.Washed, 2) : 0m,
            revenue.TipsTotal,
            volume.Washed > 0 ? Round((decimal)revenue.TicketsWithTip / volume.Washed * 100m, 1) : 0m
        );

        return new CarwashMetricsResponse(
            fromDate,
            toDate,
            volumeDto,
            revenueDto,
            FillDayGaps(daily, fromDate, toDate),
            services,
            extras,
            FillHourGaps(hourly),
            washers.Select(w => new CarwashWasherRankingDto(
                w.WasherId,
                w.WasherName,
                w.IsActive,
                w.Washed,
                w.ServicesRevenue + w.ExtrasRevenue,
                w.TipsTotal,
                ToWholeMinutes(w.AverageServiceMinutes)
            )).ToList()
        );
    }

    /// <summary>
    /// Normaliza el rango recibido: recorta la hora (el tablero razona en días
    /// completos), lo ordena si vino al revés y le pone un techo.
    /// </summary>
    private static (DateTime From, DateTime To) ResolveRange(DateTime? from, DateTime? to)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddDays(-(DefaultWindowDays - 1))).Date;

        if (fromDate > toDate)
            throw new AppValidationException("La fecha inicial no puede ser posterior a la final.");

        // Sin tope, un rango de diez años arma un gráfico de 3.650 puntos que ni
        // se lee ni se dibuja. Que lo diga el mensaje es mejor que devolver algo
        // recortado en silencio y que el dueño saque conclusiones de datos a medias.
        if ((toDate - fromDate).TotalDays + 1 > MaxWindowDays)
            throw new AppValidationException($"El rango no puede superar {MaxWindowDays} días.");

        return (fromDate, toDate);
    }

    /// <summary>
    /// Completa con ceros los días sin movimiento. Sin esto el gráfico une el
    /// último lunes con el siguiente y dibuja una línea continua sobre un fin de
    /// semana en el que el local estuvo cerrado.
    /// </summary>
    private static IReadOnlyList<CarwashDailyPointDto> FillDayGaps(
        IReadOnlyList<CarwashDailyPointDto> rows, DateTime fromDate, DateTime toDate)
    {
        var byDate = rows.ToDictionary(r => r.Date.Date);
        var result = new List<CarwashDailyPointDto>();

        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            result.Add(byDate.TryGetValue(day, out var row)
                ? row with { Date = day }
                : new CarwashDailyPointDto(day, 0, 0m));
        }

        return result;
    }

    /// <summary>Las 24 horas siempre presentes, por el mismo motivo que los días.</summary>
    private static IReadOnlyList<CarwashHourlyPointDto> FillHourGaps(IReadOnlyList<CarwashHourlyPointDto> rows)
    {
        var byHour = rows.ToDictionary(r => r.Hour);
        return Enumerable.Range(0, 24)
            .Select(h => byHour.TryGetValue(h, out var row) ? row : new CarwashHourlyPointDto(h, 0))
            .ToList();
    }

    /// <summary>
    /// Minutos enteros: al usuario no le sirve "23,4718 minutos", y redondear en
    /// el frontend obligaría a repetir la misma decisión en cada tarjeta.
    /// </summary>
    private static int? ToWholeMinutes(double? minutes) =>
        minutes is null ? null : (int)Math.Round(minutes.Value, MidpointRounding.AwayFromZero);

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
