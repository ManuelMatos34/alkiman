import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { BarbershopPortalLink, CreateBarbershopPortalLinkRequest } from "@/domain/types/barbershop"

export const barbershopPortalLinksKey = ["barbershop-portal-links"] as const

export function useBarbershopPortalLinks() {
  const api = useApiClient()
  return useQuery({
    queryKey: barbershopPortalLinksKey,
    queryFn: async () => {
      const { data } = await api.get<BarbershopPortalLink[]>("/api/barbershop/portal-links")
      return data
    },
    retry: false,
  })
}

export function useCreateBarbershopPortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: CreateBarbershopPortalLinkRequest) => {
      const { data } = await api.post<BarbershopPortalLink>("/api/barbershop/portal-links", request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopPortalLinksKey })
    },
  })
}

export function useSetBarbershopPortalLinkActive() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      const { data } = await api.put<BarbershopPortalLink>(
        `/api/barbershop/portal-links/${id}/active`,
        { isActive }
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopPortalLinksKey })
    },
  })
}

export function useDeleteBarbershopPortalLink() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/barbershop/portal-links/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: barbershopPortalLinksKey })
    },
  })
}
