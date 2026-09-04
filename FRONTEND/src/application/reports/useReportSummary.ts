import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { ReportSummary } from "@/domain/types/report"

export const reportSummaryQueryKey = ["reports", "summary"] as const

/** Tablero de métricas del negocio autenticado: finanzas, rentas, activos y rankings. */
export function useReportSummary() {
  const api = useApiClient()

  return useQuery({
    queryKey: reportSummaryQueryKey,
    queryFn: async () => {
      const { data } = await api.get<ReportSummary>("/api/reports/summary")
      return data
    },
  })
}
