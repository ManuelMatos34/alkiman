using Alkiman.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationsController(ILocationService service)
    {
        _service = service;
    }

    /// <summary>Catálogo completo de países.</summary>
    [HttpGet("countries")]
    public async Task<ActionResult<IReadOnlyList<CountryResponse>>> GetCountries(CancellationToken cancellationToken)
        => Ok(await _service.GetCountriesAsync(cancellationToken));

    /// <summary>Provincias/estados de un país.</summary>
    [HttpGet("countries/{countryId:int}/states")]
    public async Task<ActionResult<IReadOnlyList<StateResponse>>> GetStates(int countryId, CancellationToken cancellationToken)
        => Ok(await _service.GetStatesByCountryAsync(countryId, cancellationToken));

    /// <summary>Ciudades principales de una provincia/estado.</summary>
    [HttpGet("states/{stateId:int}/cities")]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> GetCities(int stateId, CancellationToken cancellationToken)
        => Ok(await _service.GetCitiesByStateAsync(stateId, cancellationToken));
}
