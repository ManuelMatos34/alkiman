import type { ReactNode } from "react"
import type { LucideIcon } from "lucide-react"
import type { PermissionCode } from "@/domain/types/permission"

/** Ítem de navegación que apunta a una ruta del módulo. */
export interface ModuleNavLink {
  /** Clave dentro del namespace i18n del módulo (ver `ModuleDefinition.i18nNamespace`). */
  labelKey: string
  /** Path relativo a la base del módulo: `/activos`, no `/alquileres/activos`. */
  to: string
  icon: LucideIcon
  /** Ver `NavLink` de react-router: si es true, solo queda activo en match exacto (no en subrutas). */
  end?: boolean
  /** Si se declara, el ítem se oculta cuando el usuario no tiene el permiso. */
  permission?: PermissionCode
}

/** Ítem colapsable que agrupa mantenimientos relacionados (ej: Contratos y sus Plantillas). */
export interface ModuleNavGroup {
  labelKey: string
  icon: LucideIcon
  children: ModuleNavLink[]
}

export type ModuleNavEntry = ModuleNavLink | ModuleNavGroup

export function isNavGroup(entry: ModuleNavEntry): entry is ModuleNavGroup {
  return "children" in entry
}

/**
 * Todo lo que la app necesita saber de un módulo: su navegación, sus rutas y su
 * chrome. Dar de alta un módulo nuevo es escribir uno de estos y sumarlo a
 * `APP_MODULES` (ver `modules/index.ts`); no hay que tocar `App.tsx`, ni copiar un
 * layout, ni copiar un sidebar.
 *
 * El backend es la fuente de verdad de qué módulos existen y cuáles compró el
 * negocio (`CFG_Modules` / `CFG_LandlordModules`). Esto es solo el cableado de UI
 * del lado del cliente: si acá falta el módulo, no hay pantallas; si sobra, igual
 * no se puede entrar porque `ModuleRoute` consulta al backend.
 */
export interface ModuleDefinition {
  /**
   * `CFG_Modules.Code`. Es también la base de las rutas (`/carwash`), por eso
   * `ModuleSelectorPage` puede linkear a `/${module.code}` sin conocer el módulo.
   */
  code: string
  /** Namespace de i18next donde viven los `labelKey` de la navegación. */
  i18nNamespace: string
  /** Clave del link "Cambiar de módulo" del pie del sidebar, dentro del mismo namespace. */
  changeModuleKey: string
  nav: ModuleNavEntry[]
  /**
   * Se monta dentro del layout, en cualquier ruta del módulo. Para diálogos de
   * configuración inicial que tienen que dispararse aunque el usuario entre directo
   * a una subruta (el caso de `CarwashOperationModeDialog`).
   */
  overlay?: ReactNode
  /**
   * `<Route>` hijas, con paths relativos a la base del módulo. Se montan dentro del
   * layout del módulo y por detrás de `ModuleRoute`.
   */
  routes: ReactNode
}
