import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { VehicleModel } from "@/domain/types/vehicleCatalog"
import { vehicleQueryKeys } from "./vehicleQueryKeys"

export function useVehicleModels(makeId: number | null) {
  return useQuery({
    queryKey: vehicleQueryKeys.models(makeId ?? 0),
    queryFn: async () => {
      const { data } = await apiClient.get<VehicleModel[]>(`/api/vehicles/makes/${makeId}/models`)
      return data
    },
    enabled: makeId !== null && makeId > 0,
    staleTime: 1000 * 60 * 60,
  })
}
