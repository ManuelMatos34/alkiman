import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Role } from "@/domain/types/role"

export const rolesQueryKey = ["roles"] as const

/** Listado de roles del negocio autenticado. */
export function useRoles() {
  const api = useApiClient()

  return useQuery({
    queryKey: rolesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Role[]>("/api/roles")
      return data
    },
  })
}
