import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { currentLandlordQueryKey } from "@/application/landlords/useCurrentLandlord"
import type { Landlord, RegisterLandlordRequest } from "@/domain/types/landlord"

/** Completa el registro del negocio (primer login tras crear la cuenta en Auth0). */
export function useRegisterLandlord() {
  const api = useApiClient()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: RegisterLandlordRequest) => {
      const { data } = await api.post<Landlord>("/api/landlords/me", request)
      return data
    },
    onSuccess: (landlord) => {
      queryClient.setQueryData(currentLandlordQueryKey, landlord)
    },
  })
}
