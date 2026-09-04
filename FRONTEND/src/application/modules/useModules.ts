import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { AppModule } from "@/domain/types/module"

export const modulesQueryKey = ["modules"] as const

/** Catálogo de módulos de la plataforma para el negocio autenticado. */
export function useModules() {
  const api = useApiClient()

  return useQuery({
    queryKey: modulesQueryKey,
    queryFn: async () => {
      const { data } = await api.get<AppModule[]>("/api/modules")
      return data
    },
  })
}
