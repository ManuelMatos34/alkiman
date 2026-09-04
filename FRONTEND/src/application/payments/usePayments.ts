import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Payment } from "@/domain/types/payment"

export const paymentsQueryKey = ["payments"] as const

/** Libro diario de ingresos y egresos del negocio autenticado. */
export function usePayments() {
  const api = useApiClient()

  return useQuery({
    queryKey: paymentsQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Payment[]>("/api/payments")
      return data
    },
  })
}
