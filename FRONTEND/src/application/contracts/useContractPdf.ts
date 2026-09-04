import { useEffect, useState } from "react"
import { useApiClient } from "@/infrastructure/auth/useApiClient"

/**
 * Descarga el PDF del contrato como blob autenticado y expone una object URL lista para
 * usar en un `<iframe src=...>` (no se puede apuntar el iframe directo a la API porque el
 * Authorization header no se puede adjuntar a una navegación de recurso).
 */
export function useContractPdf(contractId: string | undefined) {
  const api = useApiClient()
  const [url, setUrl] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [isError, setIsError] = useState(false)

  useEffect(() => {
    if (!contractId) {
      setUrl(null)
      return
    }

    let objectUrl: string | null = null
    let cancelled = false

    setIsLoading(true)
    setIsError(false)

    api
      .get<Blob>(`/api/contracts/${contractId}/pdf`, { responseType: "blob" })
      .then(({ data }) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(data)
        setUrl(objectUrl)
      })
      .catch(() => {
        if (!cancelled) setIsError(true)
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [api, contractId])

  return { url, isLoading, isError }
}
