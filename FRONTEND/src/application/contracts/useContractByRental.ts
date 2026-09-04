import { useQuery } from "@tanstack/react-query"
import { isAxiosError } from "axios"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { Contract } from "@/domain/types/contract"

/** Contrato asociado a una renta puntual (o `null` si todavía no se generó). */
export function useContractByRental(rentalId: string | undefined) {
  const api = useApiClient()

  return useQuery({
    queryKey: ["contracts", "by-rental", rentalId] as const,
    enabled: !!rentalId,
    queryFn: async () => {
      try {
        const { data } = await api.get<Contract>(`/api/contracts/by-rental/${rentalId}`)
        return data
      } catch (error) {
        if (isAxiosError(error) && error.response?.status === 404) {
          return null
        }
        throw error
      }
    },
  })
}
