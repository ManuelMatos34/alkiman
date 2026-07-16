import { Link } from "react-router-dom"
import {
  Package,
  Users,
  CalendarClock,
  Wallet,
  Tags,
  ScrollText,
  ArrowRight,
} from "lucide-react"
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"

const modules = [
  {
    label: "Activos",
    description: "Inventario de bienes disponibles para alquilar.",
    to: "/activos",
    icon: Package,
  },
  {
    label: "Clientes",
    description: "Base de clientes de tu negocio.",
    to: "/clientes",
    icon: Users,
  },
  {
    label: "Rentas",
    description: "Tablero de alquileres activos y contratos.",
    to: "/rentas",
    icon: CalendarClock,
  },
  {
    label: "Pagos",
    description: "Libro diario de ingresos y egresos.",
    to: "/pagos",
    icon: Wallet,
  },
  {
    label: "Categorías",
    description: "Organización del inventario por categoría.",
    to: "/categorias",
    icon: Tags,
  },
  {
    label: "Bitácora",
    description: "Historial de acciones sobre tu cuenta.",
    to: "/bitacora",
    icon: ScrollText,
  },
]

export function DashboardPage() {
  const { data: landlord } = useCurrentLandlord()

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          Hola, {landlord?.businessName}
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Este es el punto de partida de tu negocio en Alkiman.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {modules.map(({ label, description, to, icon: Icon }) => (
          <Link key={to} to={to}>
            <Card className="h-full border-border/60 shadow-sm transition-colors hover:border-primary/40 hover:shadow-md">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                    <Icon className="h-5 w-5" />
                  </div>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                </div>
                <CardTitle className="pt-2 text-base">{label}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
