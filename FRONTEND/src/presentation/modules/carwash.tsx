import { Route } from "react-router-dom"
import { CarFront, Wrench, Link2, Sparkles, Users, BarChart3, CreditCard } from "lucide-react"
import { PermissionCodes } from "@/domain/types/permission"
import { PermissionRoute } from "@/presentation/components/PermissionRoute"
import { CarwashOperationModeDialog } from "@/presentation/components/CarwashOperationModeDialog"
import { CarwashBoardPage } from "@/presentation/pages/CarwashBoardPage"
import { CarwashCajaPage } from "@/presentation/pages/CarwashCajaPage"
import { CarwashServicesPage } from "@/presentation/pages/CarwashServicesPage"
import { CarwashExtrasPage } from "@/presentation/pages/CarwashExtrasPage"
import { CarwashPortalLinksPage } from "@/presentation/pages/CarwashPortalLinksPage"
import { CarwashWashersPage } from "@/presentation/pages/CarwashWashersPage"
import { CarwashMetricsPage } from "@/presentation/pages/CarwashMetricsPage"
import type { ModuleDefinition } from "@/presentation/modules/types"

export const carwashModule: ModuleDefinition = {
  code: "carwash",
  i18nNamespace: "carwash",
  changeModuleKey: "nav.changeModule",

  nav: [
    { labelKey: "nav.board", to: "/", icon: CarFront, end: true, permission: PermissionCodes.CarwashBoardView },
    {
      labelKey: "nav.caja",
      to: "/caja",
      icon: CreditCard,
      permission: PermissionCodes.CarwashCaja,
    },
    // Lavadores es gente del módulo, no cuentas del sistema: por eso está acá y
    // no en la sección Usuarios de la plataforma. Ver CarwashWashersPage.
    {
      labelKey: "nav.washers",
      to: "/lavadores",
      icon: Users,
      permission: PermissionCodes.CarwashWashersView,
    },
    {
      labelKey: "nav.metrics",
      to: "/metricas",
      icon: BarChart3,
      permission: PermissionCodes.CarwashReports,
    },
    {
      labelKey: "nav.services",
      to: "/servicios",
      icon: Wrench,
      permission: PermissionCodes.CarwashCatalogView,
    },
    {
      labelKey: "nav.extras",
      to: "/agregados",
      icon: Sparkles,
      permission: PermissionCodes.CarwashCatalogView,
    },
    {
      labelKey: "nav.portalLinks",
      to: "/portal-links",
      icon: Link2,
      permission: PermissionCodes.CarwashPortalView,
    },
  ],

  // Va en el layout y no en el tablero para que la configuración inicial (Solitario vs
  // Empresa) se dispare por cualquier ruta del módulo, no solo al entrar al tablero.
  overlay: <CarwashOperationModeDialog />,

  routes: (
    <>
      <Route element={<PermissionRoute permission={PermissionCodes.CarwashBoardView} />}>
        <Route index element={<CarwashBoardPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashCaja} />}>
        <Route path="caja" element={<CarwashCajaPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashWashersView} />}>
        <Route path="lavadores" element={<CarwashWashersPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashReports} />}>
        <Route path="metricas" element={<CarwashMetricsPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashCatalogView} />}>
        <Route path="servicios" element={<CarwashServicesPage />} />
        <Route path="agregados" element={<CarwashExtrasPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashPortalView} />}>
        <Route path="portal-links" element={<CarwashPortalLinksPage />} />
      </Route>
    </>
  ),
}
