using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Locations;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LocationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Country>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, Name, IsoCode, CreatedAt
            FROM dbo.CFG_Countries
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<Country>(sql);
        return result.ToList();
    }

    public async Task<IReadOnlyList<State>> GetStatesByCountryAsync(int countryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, CountryId, Name, CreatedAt
            FROM dbo.CFG_States
            WHERE CountryId = @CountryId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<State>(sql, new { CountryId = countryId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<City>> GetCitiesByStateAsync(int stateId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, StateId, Name, CreatedAt
            FROM dbo.CFG_Cities
            WHERE StateId = @StateId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<City>(sql, new { StateId = stateId });
        return result.ToList();
    }
}
