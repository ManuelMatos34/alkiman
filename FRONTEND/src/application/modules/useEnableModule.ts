import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useApiClient } from "@/infrastructure/auth/useApiClient"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { modulesQueryKey } from "@/application/modules/useModules"

/** Habilita un módulo disponible (`isAvailable`) pero todavía no habilitado para el negocio actual. */
export function useEnableModule() {
  const api = useApiClient()
  const queryClient = useQueryClient()
  const { refreshSession } = useAuth()

  return useMutation({
    mutationFn: async (code: string) => {
      await api.post(`/api/modules/${code}/enable`)

      // Habilitar el módulo le suma permisos al rol en la base, pero los permisos viajan
      // como claims del JWT: sin reemitirlo el usuario ve el módulo en el menú y recibe
      // 403 al entrar, hasta el próximo login. Si el refresh falla no se rompe la compra,
      // que ya está hecha: se resuelve sola al volver a entrar.
      try {
        await refreshSession()
      } catch {
        // Best-effort.
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: modulesQueryKey })
    },
  })
}
