import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Me } from "@/domain/types/me"

export const meQueryKey = ["me"] as const

/** Perfil propio del usuario autenticado (identidad, rol y permisos). */
export function useMe() {
  const api = useApiClient()

  return useQuery({
    queryKey: meQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Me>("/api/me")
      return data
    },
  })
}
