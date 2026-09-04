import { Routes, Route } from "react-router-dom"
import { ProtectedRoute } from "@/presentation/components/ProtectedRoute"
import { PermissionRoute } from "@/presentation/components/PermissionRoute"
import { ModuleRoute } from "@/presentation/components/ModuleRoute"
import { PlatformLayout } from "@/presentation/layouts/PlatformLayout"
import { ModuleLayout } from "@/presentation/layouts/ModuleLayout"
import { APP_MODULES } from "@/presentation/modules"
import { LoginPage } from "@/presentation/pages/LoginPage"
import { RegisterPage } from "@/presentation/pages/RegisterPage"
import { ForgotPasswordPage } from "@/presentation/pages/ForgotPasswordPage"
import { ResetPasswordPage } from "@/presentation/pages/ResetPasswordPage"
import { ForcedChangePasswordPage } from "@/presentation/pages/ForcedChangePasswordPage"
import { ModuleSelectorPage } from "@/presentation/pages/ModuleSelectorPage"
import { SettingsPage } from "@/presentation/pages/SettingsPage"
import { UsersPage } from "@/presentation/pages/UsersPage"
import { RolesPage } from "@/presentation/pages/RolesPage"
import { PortalCatalogPage } from "@/presentation/pages/PortalCatalogPage"
import { PortalCheckoutPage } from "@/presentation/pages/PortalCheckoutPage"
import { MyRentalPage } from "@/presentation/pages/MyRentalPage"
import { CarwashPortalJoinPage } from "@/presentation/pages/CarwashPortalJoinPage"
import { CarwashTicketStatusPage } from "@/presentation/pages/CarwashTicketStatusPage"
import { NotFoundPage } from "@/presentation/pages/NotFoundPage"
import { PermissionCodes } from "@/domain/types/permission"

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/olvide-password" element={<ForgotPasswordPage />} />
      <Route path="/restablecer-password" element={<ResetPasswordPage />} />

      {/*
        Rutas públicas de cara al cliente final: no llevan sesión ni guard de módulo,
        el backend las valida por el token del link. Por eso viven acá y no en la
        `ModuleDefinition` del módulo que las origina.
      */}
      <Route path="/p/:slug" element={<PortalCatalogPage />} />
      <Route path="/p/:slug/rentar/:assetId" element={<PortalCheckoutPage />} />
      <Route path="/mi-renta/:token" element={<MyRentalPage />} />
      <Route path="/lavado/:slug" element={<CarwashPortalJoinPage />} />
      <Route path="/lavado/turno/:token" element={<CarwashTicketStatusPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/cambiar-password" element={<ForcedChangePasswordPage />} />

        {/* Plataforma: existe siempre, sin importar qué módulos compró el negocio. */}
        <Route element={<PlatformLayout />}>
          <Route index element={<ModuleSelectorPage />} />
          <Route path="ajustes" element={<SettingsPage />} />

          <Route element={<PermissionRoute permission={PermissionCodes.UsersManage} />}>
            <Route path="usuarios" element={<UsersPage />} />
          </Route>

          <Route element={<PermissionRoute permission={PermissionCodes.RolesManage} />}>
            <Route path="roles" element={<RolesPage />} />
          </Route>
        </Route>

        {/*
          Un bloque por módulo registrado, todos con la misma forma: guard de módulo
          habilitado -> layout con el sidebar del módulo -> sus rutas. Agregar un módulo
          no se toca acá: ver `presentation/modules/index.ts`.
        */}
        {APP_MODULES.map((module) => (
          <Route key={module.code} element={<ModuleRoute code={module.code} />}>
            <Route element={<ModuleLayout module={module} />}>
              <Route path={module.code}>{module.routes}</Route>
            </Route>
          </Route>
        ))}
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
