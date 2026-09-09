import { Route } from "react-router-dom"
import { Scissors, BookOpen, Users, Link2, BarChart3 } from "lucide-react"
import { PermissionCodes } from "@/domain/types/permission"
import { PermissionRoute } from "@/presentation/components/PermissionRoute"
import { BarbershopBoardPage } from "@/presentation/pages/BarbershopBoardPage"
import { BarbershopServicesPage } from "@/presentation/pages/BarbershopServicesPage"
import { BarbershopStylistsPage } from "@/presentation/pages/BarbershopStylistsPage"
import { BarbershopPortalLinksPage } from "@/presentation/pages/BarbershopPortalLinksPage"
import { BarbershopMetricsPage } from "@/presentation/pages/BarbershopMetricsPage"
import type { ModuleDefinition } from "@/presentation/modules/types"

export const barbershopModule: ModuleDefinition = {
  code: "barbershop",
  i18nNamespace: "barbershop",
  changeModuleKey: "nav.changeModule",

  nav: [
    { labelKey: "nav.board",       to: "/",            icon: Scissors,  end: true, permission: PermissionCodes.BarbershopBoardView },
    { labelKey: "nav.stylists",    to: "/estilistas",  icon: Users,               permission: PermissionCodes.BarbershopStylistsView },
    { labelKey: "nav.metrics",     to: "/metricas",    icon: BarChart3,            permission: PermissionCodes.BarbershopReports },
    { labelKey: "nav.services",    to: "/servicios",   icon: BookOpen,            permission: PermissionCodes.BarbershopCatalogView },
    { labelKey: "nav.portalLinks", to: "/portal-links", icon: Link2,              permission: PermissionCodes.BarbershopPortalView },
  ],

  routes: (
    <>
      <Route element={<PermissionRoute permission={PermissionCodes.BarbershopBoardView} />}>
        <Route index element={<BarbershopBoardPage />} />
      </Route>
      <Route element={<PermissionRoute permission={PermissionCodes.BarbershopStylistsView} />}>
        <Route path="estilistas" element={<BarbershopStylistsPage />} />
      </Route>
      <Route element={<PermissionRoute permission={PermissionCodes.BarbershopReports} />}>
        <Route path="metricas" element={<BarbershopMetricsPage />} />
      </Route>
      <Route element={<PermissionRoute permission={PermissionCodes.BarbershopCatalogView} />}>
        <Route path="servicios" element={<BarbershopServicesPage />} />
      </Route>
      <Route element={<PermissionRoute permission={PermissionCodes.BarbershopPortalView} />}>
        <Route path="portal-links" element={<BarbershopPortalLinksPage />} />
      </Route>
    </>
  ),
}
