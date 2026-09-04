import { useMutation } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import type { ChangeMyPasswordRequest } from "@/domain/types/me"

/** Cambia la contraseña propia del usuario autenticado. */
export function useChangeMyPassword() {
  const api = useApiClient()

  return useMutation({
    mutationFn: async (request: ChangeMyPasswordRequest) => {
      await api.put("/api/me/password", request)
    },
  })
}
