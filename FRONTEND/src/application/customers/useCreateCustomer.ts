import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { customersQueryKey } from "@/application/customers/useCustomers"
import type { Customer, CreateCustomerRequest } from "@/domain/types/customer"

export function useCreateCustomer() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateCustomerRequest) => {
      const { data } = await api.post<Customer>("/api/customers", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customersQueryKey })
    },
  })
}
