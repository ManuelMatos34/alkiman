import { NavLink } from "react-router-dom"
import {
  LayoutDashboard,
  Package,
  Users,
  CalendarClock,
  Wallet,
  Tags,
  ScrollText,
} from "lucide-react"
import { cn } from "@/lib/utils"

const navItems = [
  { label: "Dashboard", to: "/", icon: LayoutDashboard, end: true },
  { label: "Activos", to: "/activos", icon: Package },
  { label: "Clientes", to: "/clientes", icon: Users },
  { label: "Rentas", to: "/rentas", icon: CalendarClock },
  { label: "Pagos", to: "/pagos", icon: Wallet },
  { label: "Categorías", to: "/categorias", icon: Tags },
  { label: "Bitácora", to: "/bitacora", icon: ScrollText },
]

export function Sidebar() {
  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-border bg-white md:flex">
      <div className="flex h-16 items-center gap-2 px-6">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-sm font-semibold text-primary-foreground">
          A
        </div>
        <span className="text-base font-semibold tracking-tight">Alkiman</span>
      </div>

      <nav className="flex-1 space-y-1 px-3 py-4">
        {navItems.map(({ label, to, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-accent text-accent-foreground"
                  : "text-muted-foreground hover:bg-muted hover:text-foreground"
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
