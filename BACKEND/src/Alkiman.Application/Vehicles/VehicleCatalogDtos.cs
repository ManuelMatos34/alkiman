namespace Alkiman.Application.Vehicles;

public record VehicleMakeResponse(int Id, string Name);
public record VehicleModelResponse(int Id, int MakeId, string Name);
