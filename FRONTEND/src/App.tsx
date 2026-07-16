import { Routes, Route } from "react-router-dom"
import { ProtectedRoute } from "@/presentation/components/ProtectedRoute"
import { DashboardLayout } from "@/presentation/layouts/DashboardLayout"
import { LoginPage } from "@/presentation/pages/LoginPage"
import { DashboardPage } from "@/presentation/pages/DashboardPage"
import { ComingSoonPage } from "@/presentation/pages/ComingSoonPage"
import { NotFoundPage } from "@/presentation/pages/NotFoundPage"

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="activos" element={<ComingSoonPage title="Activos" />} />
          <Route path="clientes" element={<ComingSoonPage title="Clientes" />} />
          <Route path="rentas" element={<ComingSoonPage title="Rentas" />} />
          <Route path="pagos" element={<ComingSoonPage title="Pagos" />} />
          <Route
            path="categorias"
            element={<ComingSoonPage title="Categorías" />}
          />
          <Route path="bitacora" element={<ComingSoonPage title="Bitácora" />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
