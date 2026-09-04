import { Outlet } from "react-router-dom"
import { ModuleSidebar, ModuleSidebarNav } from "@/presentation/components/ModuleSidebar"
import { Topbar } from "@/presentation/components/Topbar"
import { AppearanceSync } from "@/presentation/components/AppearanceSync"
import type { ModuleDefinition } from "@/presentation/modules/types"

/**
 * Layout común a todos los módulos: sidebar propio del módulo + topbar general.
 * Lo que antes era un archivo por módulo (idénticos salvo el sidebar) ahora sale de
 * la `ModuleDefinition`, incluido el `overlay` para diálogos que tienen que vivir en
 * cualquier ruta del módulo.
 */
export function ModuleLayout({ module }: { module: ModuleDefinition }) {
  return (
    <div className="flex min-h-screen bg-muted/40">
      <AppearanceSync />
      {module.overlay}
      <ModuleSidebar module={module} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar
          mobileNav={(onNavigate) => <ModuleSidebarNav module={module} onNavigate={onNavigate} />}
        />
        <main className="min-w-0 flex-1 overflow-x-hidden p-4 sm:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
