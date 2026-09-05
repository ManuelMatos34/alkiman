namespace Alkiman.Application.Carwash;

/// <summary>
/// Tablero de métricas del módulo Carwash para un rango de fechas.
///
/// Todo se calcula sobre tickets ENTREGADOS (Delivered), no sobre tickets creados:
/// un vehículo que entró a la cola y se canceló no lavó nada, no facturó nada y no
/// le da trabajo a nadie. La única excepción es <see cref="Volume"/>, que necesita
/// mirar también los cancelados/expirados justamente para medir cuántos se pierden.
/// </summary>
public record CarwashMetricsResponse(
    DateTime FromDate,
    DateTime ToDate,
    CarwashVolumeSummaryDto Volume,
    CarwashRevenueSummaryDto Revenue,
    IReadOnlyList<CarwashDailyPointDto> DailyVolume,
    IReadOnlyList<CarwashServiceUsageDto> TopServices,
    IReadOnlyList<CarwashExtraUsageDto> TopExtras,
    IReadOnlyList<CarwashHourlyPointDto> HourlyDistribution,
    IReadOnlyList<CarwashWasherRankingDto> WasherRanking
);

/// <summary>Cuántos vehículos pasaron y en qué terminaron.</summary>
public record CarwashVolumeSummaryDto(
    int Washed,
    int Cancelled,
    // No-shows del portal: reservaron turno y nunca llegaron antes del
    // ArrivalDeadline. Se cuentan aparte de Cancelled porque se arreglan de
    // formas distintas (uno es recordarle al cliente, el otro es preguntarle
    // por qué se fue).
    int Expired,
    // Vehículos del rango que siguen en la cola sin cerrar. Con el rango por
    // defecto (últimos 30 días) es "los que hay ahora"; con un rango viejo son
    // tickets que quedaron colgados sin entregar ni cancelar.
    int StillInQueue,
    // (Cancelled + Expired) sobre el total de los que salieron de la cola.
    // Los dos significan lo mismo para el negocio: entró un auto y no se lavó.
    decimal CancellationRatePercent,
    // Minutos promedio entre StartedAt y DeliveredAt. NULL si ningún ticket del
    // rango llegó a tener las dos marcas.
    int? AverageServiceMinutes
);

public record CarwashRevenueSummaryDto(
    decimal ServicesRevenue,
    decimal ExtrasRevenue,
    decimal TotalRevenue,
    decimal AverageTicket,
    decimal TipsTotal,
    // Porcentaje de tickets entregados con propina > 0. Es el número que dice si
    // la política de propinas está funcionando o si sólo la anota el cajero que
    // se acuerda.
    decimal TipsCoveragePercent
);

public record CarwashDailyPointDto(DateTime Date, int Washed, decimal Revenue);

public record CarwashServiceUsageDto(int ServiceId, string ServiceName, int Count, decimal Revenue);

public record CarwashExtraUsageDto(int ExtraId, string ExtraName, int Count, decimal Revenue);

/// <summary>
/// Vehículos entregados por hora del día (0-23), agregando todos los días del rango.
/// Sirve para decidir turnos: dice a qué hora hace falta gente, que no es lo mismo
/// que cuántos autos se lavaron en total.
/// </summary>
public record CarwashHourlyPointDto(int Hour, int Washed);

public record CarwashWasherRankingDto(
    Guid WasherId,
    string WasherName,
    bool IsActive,
    int Washed,
    decimal Revenue,
    decimal TipsTotal,
    int? AverageServiceMinutes
);

// ---- Filas crudas: mapeo 1:1 con el SQL, sólo las usa el repositorio ----

public record CarwashVolumeTotalsRaw(
    int Washed,
    int Cancelled,
    int Expired,
    int StillInQueue,
    double? AverageServiceMinutes
);

public record CarwashRevenueTotalsRaw(
    decimal ServicesRevenue,
    decimal ExtrasRevenue,
    decimal TipsTotal,
    int TicketsWithTip
);

public record CarwashWasherRankingRaw(
    Guid WasherId,
    string WasherName,
    bool IsActive,
    int Washed,
    decimal ServicesRevenue,
    decimal ExtrasRevenue,
    decimal TipsTotal,
    double? AverageServiceMinutes
);
