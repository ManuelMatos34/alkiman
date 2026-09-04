import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { customersQueryKey } from "@/application/customers/useCustomers"
import type { Customer, UpdateCustomerRequest } from "@/domain/types/customer"

export function useUpdateCustomer() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateCustomerRequest }) => {
      const { data } = await api.put<Customer>(`/api/customers/${id}`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customersQueryKey })
    },
  })
}
