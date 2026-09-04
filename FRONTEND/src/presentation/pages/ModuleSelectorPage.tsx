import { Link } from "react-router-dom"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { LucideIcon } from "lucide-react"
import {
  CalendarClock,
  CalendarCheck,
  Car,
  Boxes,
  Receipt,
  ArrowRight,
  Loader2,
} from "lucide-react"
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { useModules } from "@/application/modules/useModules"
import { useEnableModule } from "@/application/modules/useEnableModule"
import { useAuth } from "@/infrastructure/auth/AuthContext"

/** Mapea el IconName que devuelve el backend (string) al ícono real de lucide-react. */
const MODULE_ICONS: Record<string, LucideIcon> = {
  CalendarClock,
  CalendarCheck,
  Car,
  Boxes,
  Receipt,
}

/**
 * Pantalla que se ve al iniciar sesión: catálogo de módulos de la plataforma.
 * Hoy solo "Alquileres" está disponible; el resto se muestra como "Próximamente".
 */
export function ModuleSelectorPage() {
  const { t } = useTranslation("modules")
  const { landlord } = useAuth()
  const { data: modules, isLoading } = useModules()
  const enableModule = useEnableModule()

  function handleEnable(code: string) {
    enableModule.mutate(code, {
      onError: () => toast.error(t("enableError")),
    })
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {t("subtitle", { name: landlord?.businessName })}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {modules?.map((module) => {
          const Icon = MODULE_ICONS[module.iconName] ?? CalendarClock
          const isClickable = module.isAvailable && module.isEnabled
          const isEnableable = module.isAvailable && !module.isEnabled

          const cardContent = (
            <Card
              className={cn(
                "h-full border-border/60 shadow-sm transition-colors",
                isClickable
                  ? "hover:border-primary/40 hover:shadow-md"
                  : !isEnableable && "cursor-not-allowed opacity-60"
              )}
            >
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                    <Icon className="h-5 w-5" />
                  </div>
                  {isClickable && <ArrowRight className="h-4 w-4 text-muted-foreground" />}
                  {!module.isAvailable && <Badge variant="secondary">{t("comingSoon")}</Badge>}
                </div>
                <CardTitle className="pt-2 text-base">{module.name}</CardTitle>
                {module.description && (
                  <CardDescription>{module.description}</CardDescription>
                )}
                {isEnableable && (
                  <Button
                    size="sm"
                    className="mt-2 w-full"
                    disabled={enableModule.isPending}
                    onClick={(event) => {
                      event.preventDefault()
                      handleEnable(module.code)
                    }}
                  >
                    {enableModule.isPending ? t("enabling") : t("enableButton")}
                  </Button>
                )}
              </CardHeader>
            </Card>
          )

          return isClickable ? (
            <Link key={module.code} to={`/${module.code}`}>
              {cardContent}
            </Link>
          ) : (
            <div key={module.code}>{cardContent}</div>
          )
        })}
      </div>
    </div>
  )
}
