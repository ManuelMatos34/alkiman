namespace Alkiman.Application.Locations;

public record CountryResponse(int Id, string Name, string IsoCode);

public record StateResponse(int Id, int CountryId, string Name);

public record CityResponse(int Id, int StateId, string Name);
