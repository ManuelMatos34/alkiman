import { useEffect, useState } from "react"
import { Link, NavLink, useLocation } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { ChevronRight, LayoutGrid } from "lucide-react"
import { cn } from "@/lib/utils"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import {
  isNavGroup,
  type ModuleDefinition,
  type ModuleNavEntry,
  type ModuleNavLink,
} from "@/presentation/modules/types"

/**
 * Los `to` de la navegación se declaran relativos al módulo y se les antepone la base
 * solo al navegar/matchear: un único punto de verdad, y mover un módulo de path no
 * obliga a reescribir cada ítem.
 */
const withModuleBase = (code: string, path: string) =>
  path === "/" ? `/${code}` : `/${code}${path}`

function matchesPath(pathname: string, code: string, link: ModuleNavLink) {
  const to = withModuleBase(code, link.to)
  return link.end ? pathname === to : pathname === to || pathname.startsWith(`${to}/`)
}

const navLinkClassName = ({ isActive }: { isActive: boolean }) =>
  cn(
    "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
    isActive
      ? "bg-accent text-accent-foreground"
      : "text-muted-foreground hover:bg-muted hover:text-foreground"
  )

interface ModuleSidebarNavProps {
  module: ModuleDefinition
  /** Se invoca al elegir un ítem; se usa para cerrar el drawer móvil. */
  onNavigate?: () => void
}

/**
 * Logo + navegación de un módulo, reutilizado por el sidebar de escritorio y el drawer
 * móvil. Es genérico: cada módulo aporta sus ítems desde su `ModuleDefinition`, así que
 * un módulo nuevo no necesita su propia copia de este archivo.
 */
export function ModuleSidebarNav({ module, onNavigate }: ModuleSidebarNavProps) {
  const { data: landlord } = useCurrentLandlord()
  const { hasPermission } = useAuth()
  const location = useLocation()
  const { t } = useTranslation(module.i18nNamespace)
  const appName = landlord?.appName ?? "Alkiman"

  // Resuelve permisos por ítem. Un grupo sin hijos visibles desaparece entero; un grupo con un
  // solo hijo visible se aplana (no tiene sentido un desplegable de un solo ítem).
  const visibleItems: ModuleNavEntry[] = module.nav.flatMap((item): ModuleNavEntry[] => {
    if (!isNavGroup(item)) {
      return !item.permission || hasPermission(item.permission) ? [item] : []
    }
    const visibleChildren = item.children.filter(
      (child) => !child.permission || hasPermission(child.permission)
    )
    if (visibleChildren.length === 0) return []
    if (visibleChildren.length === 1) return visibleChildren
    return [{ ...item, children: visibleChildren }]
  })

  const groups = visibleItems.filter(isNavGroup)

  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({})

  // Si la ruta activa cae dentro de un grupo, lo abrimos automáticamente (al entrar directo a
  // una subruta, o al navegar entre grupos sin haber tocado el desplegable todavía).
  useEffect(() => {
    setOpenGroups((prev) => {
      let changed = false
      const next = { ...prev }
      for (const group of groups) {
        const shouldOpen = group.children.some((child) =>
          matchesPath(location.pathname, module.code, child)
        )
        if (shouldOpen && !next[group.labelKey]) {
          next[group.labelKey] = true
          changed = true
        }
      }
      return changed ? next : prev
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname])

  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-6">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-sm font-semibold text-primary-foreground">
          {appName.charAt(0).toUpperCase()}
        </div>
        <span className="truncate text-base font-semibold tracking-tight">{appName}</span>
      </div>

      <nav className="flex-1 space-y-1 px-3 py-4">
        {visibleItems.map((item) => {
          if (!isNavGroup(item)) {
            return (
              <NavLink
                key={item.to}
                to={withModuleBase(module.code, item.to)}
                end={item.end}
                onClick={onNavigate}
                className={navLinkClassName}
              >
                <item.icon className="h-4 w-4" />
                {t(item.labelKey)}
              </NavLink>
            )
          }

          const isOpen = !!openGroups[item.labelKey]
          return (
            <div key={item.labelKey}>
              <button
                type="button"
                onClick={() =>
                  setOpenGroups((prev) => ({ ...prev, [item.labelKey]: !prev[item.labelKey] }))
                }
                aria-expanded={isOpen}
                className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              >
                <item.icon className="h-4 w-4" />
                <span className="flex-1 text-left">{t(item.labelKey)}</span>
                <ChevronRight
                  className={cn("h-4 w-4 shrink-0 transition-transform", isOpen && "rotate-90")}
                />
              </button>
              {isOpen && (
                <div className="mt-1 ml-3.5 space-y-1 border-l border-border pl-3.5">
                  {item.children.map((child) => (
                    <NavLink
                      key={child.to}
                      to={withModuleBase(module.code, child.to)}
                      end={child.end}
                      onClick={onNavigate}
                      className={navLinkClassName}
                    >
                      <child.icon className="h-4 w-4" />
                      {t(child.labelKey)}
                    </NavLink>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </nav>

      <div className="border-t border-border p-3">
        <Link
          to="/"
          onClick={onNavigate}
          className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
        >
          <LayoutGrid className="h-4 w-4" />
          {t(module.changeModuleKey)}
        </Link>
      </div>
    </>
  )
}

/** Sidebar fijo del módulo, visible solo desde `md` en adelante. Para pantallas chicas ver MobileSidebar. */
export function ModuleSidebar({ module }: { module: ModuleDefinition }) {
  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-border bg-card md:flex">
      <ModuleSidebarNav module={module} />
    </aside>
  )
}
