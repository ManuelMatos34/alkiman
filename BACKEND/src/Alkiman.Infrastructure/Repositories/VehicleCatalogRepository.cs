using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Vehicles;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class VehicleCatalogRepository : IVehicleCatalogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VehicleCatalogRepository(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<VehicleMake>(
            "SELECT Id, Name, IsActive FROM dbo.VH_Makes WHERE IsActive = 1 ORDER BY Name");
        return result.AsList();
    }

    public async Task<IReadOnlyList<VehicleModel>> GetModelsByMakeAsync(int makeId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<VehicleModel>(
            "SELECT Id, MakeId, Name, IsActive FROM dbo.VH_Models WHERE MakeId = @MakeId AND IsActive = 1 ORDER BY Name",
            new { MakeId = makeId });
        return result.AsList();
    }
}
