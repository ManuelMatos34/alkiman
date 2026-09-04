import { useMutation } from "@tanstack/react-query"
import { apiClient } from "@/infrastructure/http/apiClient"
import type {
  PortalContractPreviewRequest,
  PortalContractPreviewResponse,
} from "@/domain/types/portalContractPreview"

/** Previsualiza el texto del contrato (paso 2) antes de firmar y pagar. Ver nota de `usePortalCatalog` sobre por qué no usa `useApiClient`. */
export function usePortalContractPreview(slug: string | undefined) {
  return useMutation({
    mutationFn: async (request: PortalContractPreviewRequest) => {
      const { data } = await apiClient.post<PortalContractPreviewResponse>(
        `/api/portal/${slug}/contract-preview`,
        request
      )
      return data
    },
  })
}
