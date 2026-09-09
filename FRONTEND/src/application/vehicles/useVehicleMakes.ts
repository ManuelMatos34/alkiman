import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { VehicleMake } from "@/domain/types/vehicleCatalog"
import { vehicleQueryKeys } from "./vehicleQueryKeys"

export function useVehicleMakes() {
  return useQuery({
    queryKey: vehicleQueryKeys.makes(),
    queryFn: async () => {
      const { data } = await apiClient.get<VehicleMake[]>("/api/vehicles/makes")
      return data
    },
    staleTime: 1000 * 60 * 60, // 1 hora — el catálogo no cambia seguido
  })
}
