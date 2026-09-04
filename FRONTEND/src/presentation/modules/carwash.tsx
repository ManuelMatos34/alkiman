import { Route } from "react-router-dom"
import { CarFront, Wrench, Link2, Sparkles, Users } from "lucide-react"
import { PermissionCodes } from "@/domain/types/permission"
import { PermissionRoute } from "@/presentation/components/PermissionRoute"
import { CarwashOperationModeDialog } from "@/presentation/components/CarwashOperationModeDialog"
import { CarwashBoardPage } from "@/presentation/pages/CarwashBoardPage"
import { CarwashServicesPage } from "@/presentation/pages/CarwashServicesPage"
import { CarwashExtrasPage } from "@/presentation/pages/CarwashExtrasPage"
import { CarwashPortalLinksPage } from "@/presentation/pages/CarwashPortalLinksPage"
import { CarwashWashersPage } from "@/presentation/pages/CarwashWashersPage"
import type { ModuleDefinition } from "@/presentation/modules/types"

export const carwashModule: ModuleDefinition = {
  code: "carwash",
  i18nNamespace: "carwash",
  changeModuleKey: "nav.changeModule",

  nav: [
    { labelKey: "nav.board", to: "/", icon: CarFront, end: true },
    // Lavadores es gente del módulo, no cuentas del sistema: por eso está acá y
    // no en la sección Usuarios de la plataforma. Ver CarwashWashersPage.
    {
      labelKey: "nav.washers",
      to: "/lavadores",
      icon: Users,
      permission: PermissionCodes.CarwashManage,
    },
    {
      labelKey: "nav.services",
      to: "/servicios",
      icon: Wrench,
      permission: PermissionCodes.CarwashManage,
    },
    {
      labelKey: "nav.extras",
      to: "/agregados",
      icon: Sparkles,
      permission: PermissionCodes.CarwashManage,
    },
    {
      labelKey: "nav.portalLinks",
      to: "/portal-links",
      icon: Link2,
      permission: PermissionCodes.CarwashManage,
    },
  ],

  // Va en el layout y no en el tablero para que la configuración inicial (Solitario vs
  // Empresa) se dispare por cualquier ruta del módulo, no solo al entrar al tablero.
  overlay: <CarwashOperationModeDialog />,

  routes: (
    <>
      <Route element={<PermissionRoute permission={PermissionCodes.CarwashView} />}>
        <Route index element={<CarwashBoardPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.CarwashManage} />}>
        <Route path="lavadores" element={<CarwashWashersPage />} />
        <Route path="servicios" element={<CarwashServicesPage />} />
        <Route path="agregados" element={<CarwashExtrasPage />} />
        <Route path="portal-links" element={<CarwashPortalLinksPage />} />
      </Route>
    </>
  ),
}
