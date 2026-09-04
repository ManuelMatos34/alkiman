using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

public interface ICarwashTicketRepository
{
    /// <summary>Tickets activos (no Delivered/Cancelled/Expired) del negocio, ordenados por QueueNumber.</summary>
    Task<IReadOnlyList<CarwashTicket>> GetActiveByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CarwashTicket?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CarwashTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashTicket ticket, CancellationToken cancellationToken = default);

    /// <summary>Secuencia diaria por negocio: MAX(QueueNumber)+1 entre los tickets creados hoy (UTC), default 1.</summary>
    Task<int> GetNextQueueNumberAsync(Guid landlordId, CancellationToken cancellationToken = default);

    /// <summary>Guarda los extras elegidos en un ticket recién creado (snapshot de nombre y precio). Solo al dar de alta: los extras no se editan después.</summary>
    Task AddExtrasAsync(IEnumerable<CarwashTicketExtra> extras, CancellationToken cancellationToken = default);

    /// <summary>Extras de varios tickets en una sola consulta, para no hacer un round-trip por tarjeta del tablero.</summary>
    Task<IReadOnlyList<CarwashTicketExtra>> GetExtrasByTicketIdsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default);

    /// <summary>Si el extra ya se usó en algún ticket, no se puede eliminar (violaría la FK de CWS_TicketExtras) — hay que desactivarlo.</summary>
    Task<bool> HasTicketsWithExtraAsync(int extraId, CancellationToken cancellationToken = default);
}
