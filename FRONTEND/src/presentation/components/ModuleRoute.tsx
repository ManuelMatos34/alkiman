import { Outlet } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { useModules } from "@/application/modules/useModules"
import { ForbiddenPage } from "@/presentation/pages/ForbiddenPage"

interface ModuleRouteProps {
  /** `CFG_Modules.Code` del módulo dueño de las rutas anidadas. */
  code: string
}

/**
 * Gatea las rutas de un módulo por si el negocio lo tiene habilitado. Es el espejo en
 * el cliente de `RequireModuleAttribute` del backend, y responde otra pregunta que
 * `PermissionRoute`: esa mira si el *usuario* tiene el permiso, esta si el *negocio*
 * compró el módulo.
 *
 * Hace falta porque `hasPermission` devuelve true para cualquier dueño de negocio: sin
 * esto el dueño entra a un módulo que no compró, ve la pantalla armarse y recibe 403 de
 * cada request.
 *
 * Ante un error al leer el catálogo deja pasar a propósito: el backend igual cierra la
 * puerta, y es preferible una pantalla con datos vacíos a bloquear un módulo que sí se
 * compró porque falló una request secundaria.
 */
export function ModuleRoute({ code }: ModuleRouteProps) {
  const { t } = useTranslation("misc")
  const { data: modules, isLoading, isError } = useModules()

  // GlobalLoader ya cubre la pantalla mientras la consulta está en vuelo.
  if (isLoading) return null
  if (isError) return <Outlet />

  const module = modules?.find((item) => item.code === code)
  if (!module?.isEnabled) {
    return <ForbiddenPage description={t("moduleNotEnabled.description")} />
  }

  return <Outlet />
}
