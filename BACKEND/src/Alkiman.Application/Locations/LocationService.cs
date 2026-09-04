namespace Alkiman.Application.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _repository;

    public LocationService(ILocationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CountryResponse>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var countries = await _repository.GetAllCountriesAsync(cancellationToken);
        return countries.Select(c => new CountryResponse(c.Id, c.Name, c.IsoCode)).ToList();
    }

    public async Task<IReadOnlyList<StateResponse>> GetStatesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        var states = await _repository.GetStatesByCountryAsync(countryId, cancellationToken);
        return states.Select(s => new StateResponse(s.Id, s.CountryId, s.Name)).ToList();
    }

    public async Task<IReadOnlyList<CityResponse>> GetCitiesByStateAsync(int stateId, CancellationToken cancellationToken = default)
    {
        var cities = await _repository.GetCitiesByStateAsync(stateId, cancellationToken);
        return cities.Select(c => new CityResponse(c.Id, c.StateId, c.Name)).ToList();
    }
}
