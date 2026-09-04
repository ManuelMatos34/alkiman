namespace Alkiman.Application.Locations;

public interface ILocationService
{
    Task<IReadOnlyList<CountryResponse>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StateResponse>> GetStatesByCountryAsync(int countryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityResponse>> GetCitiesByStateAsync(int stateId, CancellationToken cancellationToken = default);
}
