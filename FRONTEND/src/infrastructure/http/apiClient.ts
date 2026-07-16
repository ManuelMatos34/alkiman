import axios from "axios"

/**
 * Cliente HTTP base para la API de Alkiman.
 * No lleva el token de autenticación incorporado: lo agrega
 * el interceptor de `useApiClient` en tiempo de request.
 */
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})
