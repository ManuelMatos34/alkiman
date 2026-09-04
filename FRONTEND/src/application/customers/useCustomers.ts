import { useQuery } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Customer } from "@/domain/types/customer"

export const customersQueryKey = ["customers"] as const

/** Listado de clientes (inquilinos) del negocio autenticado. */
export function useCustomers() {
  const api = useApiClient()

  return useQuery({
    queryKey: customersQueryKey,
    queryFn: async () => {
      const { data } = await api.get<Customer[]>("/api/customers")
      return data
    },
  })
}
