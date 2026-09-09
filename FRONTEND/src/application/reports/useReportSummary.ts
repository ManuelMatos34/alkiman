import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { ReportSummary } from "@/domain/types/report"

export const reportSummaryQueryKey = (from?: string, to?: string) =>
  ["reports", "summary", from ?? null, to ?? null] as const

/** Tablero de métricas del negocio: finanzas, rentas, activos y rankings. */
export function useReportSummary(from?: string, to?: string) {
  const api = useApiClient()

  return useQuery({
    queryKey: reportSummaryQueryKey(from, to),
    queryFn: async () => {
      const { data } = await api.get<ReportSummary>("/api/reports/summary", {
        params: { from, to },
      })
      return data
    },
    // Agregaciones históricas: no cambian entre pestañeos.
    staleTime: 60_000,
  })
}
