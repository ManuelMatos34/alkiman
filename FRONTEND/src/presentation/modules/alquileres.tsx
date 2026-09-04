import { Route } from "react-router-dom"
import {
  LayoutDashboard,
  Package,
  Layers,
  Boxes,
  Users,
  CalendarClock,
  Wallet,
  Tags,
  BarChart3,
  Mail,
  Link2,
  FileText,
  FileSignature,
  FileClock,
} from "lucide-react"
import { PermissionCodes } from "@/domain/types/permission"
import { PermissionRoute } from "@/presentation/components/PermissionRoute"
import { DashboardPage } from "@/presentation/pages/DashboardPage"
import { AssetsPage } from "@/presentation/pages/AssetsPage"
import { AssetGroupsPage } from "@/presentation/pages/AssetGroupsPage"
import { CategoriesPage } from "@/presentation/pages/CategoriesPage"
import { CustomersPage } from "@/presentation/pages/CustomersPage"
import { RentalsPage } from "@/presentation/pages/RentalsPage"
import { RentalImportPage } from "@/presentation/pages/RentalImportPage"
import { PaymentsPage } from "@/presentation/pages/PaymentsPage"
import { ReportsPage } from "@/presentation/pages/ReportsPage"
import { EmailsPage } from "@/presentation/pages/EmailsPage"
import { PortalLinksPage } from "@/presentation/pages/PortalLinksPage"
import { ContractTemplatesPage } from "@/presentation/pages/ContractTemplatesPage"
import { ContractsPage } from "@/presentation/pages/ContractsPage"
import { RentalRequestsPage } from "@/presentation/pages/RentalRequestsPage"
import type { ModuleDefinition } from "@/presentation/modules/types"

export const alquileresModule: ModuleDefinition = {
  code: "alquileres",
  i18nNamespace: "nav",
  changeModuleKey: "changeModule",

  /**
   * Mantenimientos relacionados se agrupan bajo un mismo ítem colapsable (ej: Contratos y sus
   * Plantillas). Los que no tienen una relación clara con otro quedan como ítems sueltos.
   */
  nav: [
    { labelKey: "dashboard", to: "/", icon: LayoutDashboard, end: true },
    {
      labelKey: "inventory",
      icon: Boxes,
      children: [
        { labelKey: "assets", to: "/activos", icon: Package, permission: PermissionCodes.AssetsView },
        {
          labelKey: "assetGroups",
          to: "/grupos-activos",
          icon: Layers,
          permission: PermissionCodes.AssetGroupsView,
        },
        {
          labelKey: "categories",
          to: "/categorias",
          icon: Tags,
          permission: PermissionCodes.CategoriesView,
        },
      ],
    },
    {
      labelKey: "customers",
      to: "/clientes",
      icon: Users,
      permission: PermissionCodes.CustomersView,
    },
    {
      labelKey: "rentalsGroup",
      icon: CalendarClock,
      children: [
        {
          labelKey: "rentals",
          to: "/rentas",
          icon: CalendarClock,
          end: true,
          permission: PermissionCodes.RentalsView,
        },
        {
          labelKey: "rentalRequests",
          to: "/pedidos-renta",
          icon: FileClock,
          permission: PermissionCodes.RentalRequestsView,
        },
        {
          labelKey: "payments",
          to: "/pagos",
          icon: Wallet,
          permission: PermissionCodes.PaymentsView,
        },
      ],
    },
    { labelKey: "reports", to: "/metricas", icon: BarChart3, permission: PermissionCodes.ReportsView },
    { labelKey: "emails", to: "/correos", icon: Mail, permission: PermissionCodes.EmailsView },
    { labelKey: "portal", to: "/portal-links", icon: Link2, permission: PermissionCodes.PortalView },
    {
      labelKey: "contractsGroup",
      icon: FileText,
      children: [
        {
          labelKey: "contracts",
          to: "/contratos",
          icon: FileText,
          end: true,
          permission: PermissionCodes.ContractsView,
        },
        {
          labelKey: "templates",
          to: "/contratos/plantillas",
          icon: FileSignature,
          permission: PermissionCodes.ContractsView,
        },
      ],
    },
  ],

  routes: (
    <>
      <Route index element={<DashboardPage />} />
      <Route path="activos" element={<AssetsPage />} />
      <Route path="grupos-activos" element={<AssetGroupsPage />} />
      <Route path="clientes" element={<CustomersPage />} />
      <Route path="rentas" element={<RentalsPage />} />

      <Route element={<PermissionRoute permission={PermissionCodes.RentalsManage} />}>
        <Route path="rentas/importar" element={<RentalImportPage />} />
      </Route>

      <Route path="pagos" element={<PaymentsPage />} />
      <Route path="categorias" element={<CategoriesPage />} />
      <Route path="metricas" element={<ReportsPage />} />

      <Route element={<PermissionRoute permission={PermissionCodes.EmailsView} />}>
        <Route path="correos" element={<EmailsPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.PortalView} />}>
        <Route path="portal-links" element={<PortalLinksPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.ContractsView} />}>
        <Route path="contratos/plantillas" element={<ContractTemplatesPage />} />
        <Route path="contratos" element={<ContractsPage />} />
      </Route>

      <Route element={<PermissionRoute permission={PermissionCodes.RentalRequestsView} />}>
        <Route path="pedidos-renta" element={<RentalRequestsPage />} />
      </Route>
    </>
  ),
}
