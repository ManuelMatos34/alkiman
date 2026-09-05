using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

/// <summary>
/// Agregaciones del tablero de Carwash.
///
/// Regla que se repite en casi todas las consultas: la unidad de medida es el
/// ticket ENTREGADO, fechado por DeliveredAt y no por CreatedAt. Un vehículo que
/// entró a la cola el lunes y se entregó el martes es facturación del martes, que
/// es cuando el negocio cobró. Fechar por creación desalinearía los ingresos de la
/// caja del día.
/// </summary>
public class CarwashMetricsRepository : ICarwashMetricsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashMetricsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Filtro de los tickets entregados dentro del rango. Rango semiabierto
    /// [From, To): con BETWEEN habría que pasar 23:59:59.999 y se perderían los
    /// tickets entregados en el último milisegundo del día.
    /// </summary>
    private const string DeliveredInRange = """
        t.LandlordId = @LandlordId
        AND t.Status = 'Delivered'
        AND t.DeliveredAt >= @From
        AND t.DeliveredAt < @To
        """;

    public async Task<CarwashVolumeTotalsRaw> GetVolumeTotalsAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Cada estado se fecha por SU propia marca de tiempo. 'Expired' no tiene
        // columna propia (nadie hace nada para que un ticket expire: simplemente
        // se le pasa el ArrivalDeadline), así que se fecha por ese vencimiento,
        // que es el instante en el que efectivamente se perdió el turno.
        const string sql = """
            SELECT
                Washed     = COUNT(CASE WHEN t.Status = 'Delivered' AND t.DeliveredAt >= @From AND t.DeliveredAt < @To THEN 1 END),
                Cancelled  = COUNT(CASE WHEN t.Status = 'Cancelled' AND t.CancelledAt >= @From AND t.CancelledAt < @To THEN 1 END),
                Expired    = COUNT(CASE WHEN t.Status = 'Expired'
                                         AND ISNULL(t.ArrivalDeadline, t.CreatedAt) >= @From
                                         AND ISNULL(t.ArrivalDeadline, t.CreatedAt) <  @To THEN 1 END),
                StillInQueue = COUNT(CASE WHEN t.Status NOT IN ('Delivered','Cancelled','Expired')
                                          AND t.CreatedAt >= @From AND t.CreatedAt < @To THEN 1 END),
                -- Segundos y no minutos en el DATEDIFF: con minutos, un lavado de
                -- 100 segundos cuenta como 1 y el promedio se va al piso.
                AverageServiceMinutes = AVG(CASE
                    WHEN t.Status = 'Delivered' AND t.DeliveredAt >= @From AND t.DeliveredAt < @To AND t.StartedAt IS NOT NULL
                    THEN CAST(DATEDIFF(SECOND, t.StartedAt, t.DeliveredAt) AS FLOAT) / 60.0
                END)
            FROM dbo.CWS_Tickets t
            WHERE t.LandlordId = @LandlordId
            """;

        return await connection.QuerySingleAsync<CarwashVolumeTotalsRaw>(sql, new { LandlordId = landlordId, From = from, To = to });
    }

    public async Task<CarwashRevenueTotalsRaw> GetRevenueTotalsAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // ISNULL sobre cada SUM porque sobre un conjunto vacío SUM devuelve NULL,
        // y del otro lado hay un decimal no anulable: un negocio recién habilitado
        // reventaría el tablero en vez de mostrar ceros.
        const string sql = $"""
            SELECT
                ServicesRevenue = ISNULL(SUM(t.ServicePrice), 0),
                ExtrasRevenue   = ISNULL(SUM(e.ExtrasTotal), 0),
                TipsTotal       = ISNULL(SUM(t.TipAmount), 0),
                TicketsWithTip  = COUNT(CASE WHEN t.TipAmount > 0 THEN 1 END)
            FROM dbo.CWS_Tickets t
            OUTER APPLY (
                SELECT ExtrasTotal = SUM(x.Price)
                FROM dbo.CWS_TicketExtras x
                WHERE x.TicketId = t.Id
            ) e
            WHERE {DeliveredInRange}
            """;

        return await connection.QuerySingleAsync<CarwashRevenueTotalsRaw>(sql, new { LandlordId = landlordId, From = from, To = to });
    }

    public async Task<IReadOnlyList<CarwashDailyPointDto>> GetDailyVolumeAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Sin días vacíos: los rellena el servicio, que es quien conoce el rango
        // completo. Traerlos desde SQL obligaría a un CTE recursivo de calendario
        // para algo que en C# son tres líneas.
        const string sql = $"""
            SELECT
                Date    = CAST(t.DeliveredAt AS DATE),
                Washed  = COUNT(*),
                Revenue = ISNULL(SUM(t.ServicePrice), 0) + ISNULL(SUM(e.ExtrasTotal), 0)
            FROM dbo.CWS_Tickets t
            OUTER APPLY (
                SELECT ExtrasTotal = SUM(x.Price)
                FROM dbo.CWS_TicketExtras x
                WHERE x.TicketId = t.Id
            ) e
            WHERE {DeliveredInRange}
            GROUP BY CAST(t.DeliveredAt AS DATE)
            ORDER BY CAST(t.DeliveredAt AS DATE)
            """;

        var rows = await connection.QueryAsync<CarwashDailyPointDto>(sql, new { LandlordId = landlordId, From = from, To = to });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CarwashServiceUsageDto>> GetTopServicesAsync(Guid landlordId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // El nombre sale del catálogo (CWS_Services), no del ticket: el ticket sólo
        // congela el PRECIO. Si al servicio le corrigen una falta de ortografía,
        // el histórico no debe quedar partido en dos filas del ranking.
        const string sql = $"""
            SELECT TOP (@Top)
                ServiceId   = t.ServiceId,
                ServiceName = s.Name,
                Count       = COUNT(*),
                Revenue     = ISNULL(SUM(t.ServicePrice), 0)
            FROM dbo.CWS_Tickets t
            INNER JOIN dbo.CWS_Services s ON s.Id = t.ServiceId
            WHERE {DeliveredInRange}
            GROUP BY t.ServiceId, s.Name
            ORDER BY COUNT(*) DESC, s.Name
            """;

        var rows = await connection.QueryAsync<CarwashServiceUsageDto>(sql, new { LandlordId = landlordId, From = from, To = to, Top = top });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CarwashExtraUsageDto>> GetTopExtrasAsync(Guid landlordId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Acá sí se agrupa por el nombre congelado en CWS_TicketExtras y no se
        // toca el catálogo: un extra se puede borrar (HasTicketsWithExtraAsync sólo
        // lo impide mientras esté en uso), y el ranking tiene que seguir sabiendo
        // cómo se llamaba.
        const string sql = $"""
            SELECT TOP (@Top)
                ExtraId   = x.ExtraId,
                ExtraName = MAX(x.Name),
                Count     = COUNT(*),
                Revenue   = ISNULL(SUM(x.Price), 0)
            FROM dbo.CWS_TicketExtras x
            INNER JOIN dbo.CWS_Tickets t ON t.Id = x.TicketId
            WHERE {DeliveredInRange}
            GROUP BY x.ExtraId
            ORDER BY COUNT(*) DESC, MAX(x.Name)
            """;

        var rows = await connection.QueryAsync<CarwashExtraUsageDto>(sql, new { LandlordId = landlordId, From = from, To = to, Top = top });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CarwashHourlyPointDto>> GetHourlyDistributionAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = $"""
            SELECT
                Hour   = DATEPART(HOUR, t.DeliveredAt),
                Washed = COUNT(*)
            FROM dbo.CWS_Tickets t
            WHERE {DeliveredInRange}
            GROUP BY DATEPART(HOUR, t.DeliveredAt)
            ORDER BY DATEPART(HOUR, t.DeliveredAt)
            """;

        var rows = await connection.QueryAsync<CarwashHourlyPointDto>(sql, new { LandlordId = landlordId, From = from, To = to });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CarwashWasherRankingRaw>> GetWasherRankingAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // El trabajo y las propinas se agregan por COLUMNAS DISTINTAS y por eso van
        // en dos CTE separados: el trabajo por AssignedToWasherId (quien lavó) y la
        // propina por TipWasherId (a quien se le atribuyó al entregar). Casi siempre
        // coinciden, pero no tienen por qué: la propina se congela y la asignación
        // sigue siendo editable.
        const string sql = $"""
            WITH Delivered AS (
                SELECT t.Id, t.AssignedToWasherId, t.TipWasherId, t.ServicePrice,
                       t.TipAmount, t.StartedAt, t.DeliveredAt
                FROM dbo.CWS_Tickets t
                WHERE {DeliveredInRange}
            ),
            WasherWork AS (
                SELECT
                    WasherId        = d.AssignedToWasherId,
                    Washed          = COUNT(*),
                    ServicesRevenue = ISNULL(SUM(d.ServicePrice), 0),
                    ExtrasRevenue   = ISNULL(SUM(e.ExtrasTotal), 0),
                    AverageServiceMinutes = AVG(CASE
                        WHEN d.StartedAt IS NOT NULL
                        THEN CAST(DATEDIFF(SECOND, d.StartedAt, d.DeliveredAt) AS FLOAT) / 60.0
                    END)
                FROM Delivered d
                OUTER APPLY (
                    SELECT ExtrasTotal = SUM(x.Price)
                    FROM dbo.CWS_TicketExtras x
                    WHERE x.TicketId = d.Id
                ) e
                WHERE d.AssignedToWasherId IS NOT NULL
                GROUP BY d.AssignedToWasherId
            ),
            WasherTips AS (
                SELECT
                    WasherId  = d.TipWasherId,
                    TipsTotal = ISNULL(SUM(d.TipAmount), 0)
                FROM Delivered d
                WHERE d.TipWasherId IS NOT NULL
                GROUP BY d.TipWasherId
            )
            SELECT
                WasherId        = w.Id,
                WasherName      = w.FullName,
                IsActive        = w.IsActive,
                Washed          = ISNULL(wk.Washed, 0),
                ServicesRevenue = ISNULL(wk.ServicesRevenue, 0),
                ExtrasRevenue   = ISNULL(wk.ExtrasRevenue, 0),
                TipsTotal       = ISNULL(tp.TipsTotal, 0),
                AverageServiceMinutes = wk.AverageServiceMinutes
            FROM dbo.CWS_Washers w
            LEFT JOIN WasherWork wk ON wk.WasherId = w.Id
            LEFT JOIN WasherTips tp ON tp.WasherId = w.Id
            -- Sólo quien hizo algo en el rango. Listar a los lavadores en cero
            -- convierte el ranking en el padrón completo del personal y esconde
            -- la información abajo del scroll.
            WHERE w.LandlordId = @LandlordId
              AND (wk.WasherId IS NOT NULL OR tp.WasherId IS NOT NULL)
            ORDER BY ISNULL(wk.Washed, 0) DESC, ISNULL(tp.TipsTotal, 0) DESC, w.FullName
            """;

        var rows = await connection.QueryAsync<CarwashWasherRankingRaw>(sql, new { LandlordId = landlordId, From = from, To = to });
        return rows.ToList();
    }
}
