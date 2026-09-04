import { Link } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"

interface ForbiddenPageProps {
  /** Reemplaza el texto por defecto ("no tienes permiso"): lo usa ModuleRoute, donde el problema no es el permiso del usuario sino el módulo del negocio. */
  description?: string
}

export function ForbiddenPage({ description }: ForbiddenPageProps) {
  const { t } = useTranslation("misc")
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 py-24 text-center">
      <div>
        <h1 className="text-4xl font-semibold tracking-tight">{t("forbidden.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {description ?? t("forbidden.description")}
        </p>
      </div>
      <Button asChild size="sm">
        <Link to="/">{t("forbidden.backHome")}</Link>
      </Button>
    </div>
  )
}
