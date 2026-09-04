import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Permission } from "@/domain/types/permission"

export const permissionsQueryKey = ["permissions"] as const

/** Catálogo completo de permisos disponibles (para el editor de roles). */
export function usePermissions() {
  const api = useApiClient()

  return useQuery({
    queryKey: permissionsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Permission[]>("/api/permissions")
      return data
    },
  })
}
