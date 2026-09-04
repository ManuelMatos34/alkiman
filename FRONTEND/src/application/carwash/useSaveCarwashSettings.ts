import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { carwashSettingsQueryKey } from "@/application/carwash/useCarwashSettings"
import { rolesQueryKey } from "@/application/roles/useRoles"
import type { CarwashSettings, SaveCarwashSettingsRequest } from "@/domain/types/carwash"

/**
 * Guarda el modo de operación. En modo Empresa el backend además siembra el rol "Lavador"
 * (rol de sistema, no borrable), así que hay que invalidar también la lista de roles.
 */
export function useSaveCarwashSettings() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: SaveCarwashSettingsRequest) => {
      const { data } = await api.put<CarwashSettings>("/api/carwash/settings", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: carwashSettingsQueryKey })
      queryClient.invalidateQueries({ queryKey: rolesQueryKey })
    },
  })
}
