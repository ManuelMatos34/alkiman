import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { BarbershopMetrics } from "@/domain/types/barbershop"

export const barbershopMetricsKey = (from: string, to: string) =>
  ["barbershop-metrics", from, to] as const

export function useBarbershopMetrics(from: string, to: string) {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopMetricsKey(from, to),
    queryFn: async () => {
      const { data } = await api.get<BarbershopMetrics>("/api/barbershop/metrics", {
        params: { from, to },
      })
      return data
    },
    staleTime: 60_000,
  })
}
