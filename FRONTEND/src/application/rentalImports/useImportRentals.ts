import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { rentalsQueryKey } from "@/application/rentals/useRentals"
import { assetsQueryKey } from "@/application/assets/useAssets"
import { customersQueryKey } from "@/application/customers/useCustomers"
import { categoriesQueryKey } from "@/application/categories/useCategories"
import type { ImportRentalsRequest, ImportRentalsResponse } from "@/domain/types/rentalImport"

/** Importación masiva de alquileres existentes: puede crear clientes, categorías y activos nuevos. */
export function useImportRentals() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: ImportRentalsRequest) => {
      const { data } = await api.post<ImportRentalsResponse>("/api/rentals/import", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: rentalsQueryKey })
      queryClient.invalidateQueries({ queryKey: assetsQueryKey })
      queryClient.invalidateQueries({ queryKey: customersQueryKey })
      queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
    },
  })
}
