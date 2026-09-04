import { useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { apiClient } from "@/infrastructure/http/apiClient"
import { useAuth } from "@/infrastructure/auth/AuthContext"

/**
 * Devuelve el cliente HTTP de la API ya configurado para adjuntar
 * el JWT propio en cada request (header Authorization: Bearer ...).
 * Si el servidor responde 401 (token vencido/inválido), cierra la
 * sesión local y redirige a /login.
 */
export function useApiClient() {
  const { token, logout } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    const requestInterceptorId = apiClient.interceptors.request.use((config) => {
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    const responseInterceptorId = apiClient.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          logout()
          navigate("/login", { replace: true })
        }
        return Promise.reject(error)
      }
    )

    return () => {
      apiClient.interceptors.request.eject(requestInterceptorId)
      apiClient.interceptors.response.eject(responseInterceptorId)
    }
  }, [token, logout, navigate])

  return apiClient
}
