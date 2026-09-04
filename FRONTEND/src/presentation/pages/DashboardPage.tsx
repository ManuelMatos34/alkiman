import { Link } from "react-router-dom"
import {
  Package,
  Users,
  CalendarClock,
  Wallet,
  Tags,
  ArrowRight,
} from "lucide-react"
import { useTranslation } from "react-i18next"
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useAuth } from "@/infrastructure/auth/AuthContext"

const modules = [
  {
    key: "assets",
    to: "/alquileres/activos",
    icon: Package,
  },
  {
    key: "customers",
    to: "/alquileres/clientes",
    icon: Users,
  },
  {
    key: "rentals",
    to: "/alquileres/rentas",
    icon: CalendarClock,
  },
  {
    key: "payments",
    to: "/alquileres/pagos",
    icon: Wallet,
  },
  {
    key: "categories",
    to: "/alquileres/categorias",
    icon: Tags,
  },
] as const

export function DashboardPage() {
  const { t } = useTranslation("dashboard")
  const { landlord } = useAuth()

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("greeting", { name: landlord?.businessName })}
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {t("subtitle")}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {modules.map(({ key, to, icon: Icon }) => (
          <Link key={to} to={to}>
            <Card className="h-full border-border/60 shadow-sm transition-colors hover:border-primary/40 hover:shadow-md">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                    <Icon className="h-5 w-5" />
                  </div>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                </div>
                <CardTitle className="pt-2 text-base">
                  {t(`modules.${key}.label`)}
                </CardTitle>
                <CardDescription>
                  {t(`modules.${key}.description`)}
                </CardDescription>
              </CardHeader>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
