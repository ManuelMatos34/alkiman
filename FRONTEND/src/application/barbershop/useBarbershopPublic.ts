import { useMutation, useQuery } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type { BarbershopPublicLink, BarbershopBookingResponse, BarbershopAppointmentStatusResponse, BookBarbershopAppointmentRequest } from "@/domain/types/barbershop"

/**
 * Info pública de un link de barbería. Usa el cliente HTTP base SIN el interceptor de auth:
 * esta pantalla se renderiza fuera de ProtectedRoute, no hay JWT ni sesión.
 */
export function useBarbershopPublicLink(slug: string | undefined) {
  return useQuery({
    queryKey: ["barbershop-public-link", slug],
    queryFn: async () => {
      const { data } = await apiClient.get<BarbershopPublicLink>(`/api/barbershop/public/${slug}`)
      return data
    },
    enabled: !!slug,
    retry: false,
  })
}

export function useBookBarbershopAppointment(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: BookBarbershopAppointmentRequest) => {
      const { data } = await apiClient.post<BarbershopBookingResponse>(
        `/api/barbershop/public/${slug}/book`,
        request
      )
      return data
    },
  })
}

export function useBarbershopAppointmentStatus(token: string | undefined) {
  return useQuery({
    queryKey: ["barbershop-appointment-status", token],
    queryFn: async () => {
      const { data } = await apiClient.get<BarbershopAppointmentStatusResponse>(
        `/api/barbershop/public/appointment/${token}`
      )
      return data
    },
    enabled: !!token,
    retry: false,
    refetchInterval: 30_000,
  })
}
