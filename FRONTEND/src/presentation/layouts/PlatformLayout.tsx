import { Outlet } from "react-router-dom"
import { Topbar } from "@/presentation/components/Topbar"
import { AppearanceSync } from "@/presentation/components/AppearanceSync"

/**
 * Layout general de la plataforma: sin sidebar de módulo. Se usa para el selector de módulos y
 * para las secciones globales (Ajustes, Usuarios, Roles) que aplican sin importar el módulo.
 */
export function PlatformLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-muted/40">
      <AppearanceSync />
      <Topbar />
      <main className="min-w-0 flex-1 overflow-x-hidden p-4 sm:p-6">
        <Outlet />
      </main>
    </div>
  )
}
