import type { ReactNode } from "react"
import { Auth0Provider, type AppState } from "@auth0/auth0-react"
import { useNavigate } from "react-router-dom"
import { auth0Config } from "@/infrastructure/auth/auth0Config"

/**
 * Envuelve Auth0Provider y usa el router para volver a la ruta original
 * después del login (en vez de recargar siempre en "/").
 * Debe montarse dentro de <BrowserRouter>.
 */
export function Auth0ProviderWithNavigate({
  children,
}: {
  children: ReactNode
}) {
  const navigate = useNavigate()

  function onRedirectCallback(appState?: AppState) {
    navigate(appState?.returnTo ?? "/")
  }

  return (
    <Auth0Provider
      domain={auth0Config.domain}
      clientId={auth0Config.clientId}
      authorizationParams={{
        redirect_uri: `${window.location.origin}/`,
        audience: auth0Config.audience,
      }}
      onRedirectCallback={onRedirectCallback}
    >
      {children}
    </Auth0Provider>
  )
}
