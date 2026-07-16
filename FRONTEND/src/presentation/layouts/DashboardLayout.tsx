import { Outlet } from "react-router-dom"
import { Sidebar } from "@/presentation/components/Sidebar"
import { Topbar } from "@/presentation/components/Topbar"
import { OnboardingGate } from "@/presentation/components/OnboardingGate"

export function DashboardLayout() {
  return (
    <div className="flex min-h-screen bg-muted/40">
      <Sidebar />
      <div className="flex flex-1 flex-col">
        <Topbar />
        <main className="flex-1 p-6">
          <OnboardingGate>
            <Outlet />
          </OnboardingGate>
        </main>
      </div>
    </div>
  )
}
