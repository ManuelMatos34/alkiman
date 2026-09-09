using Alkiman.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/vehicles")]
[AllowAnonymous]
public class VehicleCatalogController : ControllerBase
{
    private readonly IVehicleCatalogRepository _repo;

    public VehicleCatalogController(IVehicleCatalogRepository repo) => _repo = repo;

    [HttpGet("makes")]
    public async Task<ActionResult<IReadOnlyList<VehicleMakeResponse>>> GetMakes(CancellationToken cancellationToken)
    {
        var makes = await _repo.GetMakesAsync(cancellationToken);
        return Ok(makes.Select(m => new VehicleMakeResponse(m.Id, m.Name)).ToList());
    }

    [HttpGet("makes/{makeId:int}/models")]
    public async Task<ActionResult<IReadOnlyList<VehicleModelResponse>>> GetModels(int makeId, CancellationToken cancellationToken)
    {
        var models = await _repo.GetModelsByMakeAsync(makeId, cancellationToken);
        return Ok(models.Select(m => new VehicleModelResponse(m.Id, m.MakeId, m.Name)).ToList());
    }
}
