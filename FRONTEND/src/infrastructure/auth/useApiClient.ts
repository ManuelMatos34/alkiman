import { useEffect } from "react"
import { useAuth0 } from "@auth0/auth0-react"
import { apiClient } from "@/infrastructure/http/apiClient"

/**
 * Devuelve el cliente HTTP de la API ya configurado para adjuntar
 * el access token de Auth0 en cada request (header Authorization: Bearer ...).
 */
export function useApiClient() {
  const { getAccessTokenSilently } = useAuth0()

  useEffect(() => {
    const interceptorId = apiClient.interceptors.request.use(async (config) => {
      const token = await getAccessTokenSilently()
      config.headers.Authorization = `Bearer ${token}`
      return config
    })

    return () => {
      apiClient.interceptors.request.eject(interceptorId)
    }
  }, [getAccessTokenSilently])

  return apiClient
}
