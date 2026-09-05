import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { CarwashMetrics } from "@/domain/types/carwash"

/** El rango va en la key: cambiar de fechas es otra consulta, no la misma cacheada. */
export const carwashMetricsQueryKey = (from: string, to: string) =>
  ["carwash-metrics", from, to] as const

/**
 * Tablero de métricas del módulo. `from` y `to` son fechas `YYYY-MM-DD` inclusivas.
 */
export function useCarwashMetrics(from: string, to: string) {
  const api = useApiClient()

  return useQuery({
    queryKey: carwashMetricsQueryKey(from, to),
    queryFn: async () => {
      const { data } = await api.get<CarwashMetrics>("/api/carwash/metrics", {
        params: { from, to },
      })
      return data
    },
    // Son agregaciones históricas: no cambian entre un pestañeo y otro, y
    // recalcularlas en cada foco de ventana es pegarle a la base por nada.
    staleTime: 60_000,
  })
}
