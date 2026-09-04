using Alkiman.Domain.Entities;

namespace Alkiman.Application.Locations;

public interface ILocationRepository
{
    Task<IReadOnlyList<Country>> GetAllCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<State>> GetStatesByCountryAsync(int countryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> GetCitiesByStateAsync(int stateId, CancellationToken cancellationToken = default);
}
