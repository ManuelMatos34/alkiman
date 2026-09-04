import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashLinkableUser } from "@/domain/types/carwash"

export const carwashLinkableUsersQueryKey = ["carwash-linkable-users"] as const

/**
 * Cuentas del negocio que se le pueden vincular a un lavador.
 *
 * `washerId` es el lavador que se está editando: el backend lo usa para no
 * excluir su propia cuenta de la lista (si no, al abrir el formulario la cuenta
 * ya vinculada desaparecería del desplegable).
 */
export function useCarwashLinkableUsers(washerId?: string | null, enabled = true) {
  const api = useApiClient()

  return useQuery({
    queryKey: [...carwashLinkableUsersQueryKey, washerId ?? null],
    queryFn: async () => {
      const { data } = await api.get<CarwashLinkableUser[]>("/api/carwash/washers/linkable-users", {
        params: washerId ? { washerId } : undefined,
      })
      return data
    },
    enabled,
    retry: false,
  })
}
