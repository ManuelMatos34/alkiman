import { Link } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"

export function NotFoundPage() {
  const { t } = useTranslation("misc")
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background text-center">
      <div>
        <h1 className="text-4xl font-semibold tracking-tight">{t("notFound.title")}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {t("notFound.description")}
        </p>
      </div>
      <Button asChild size="sm">
        <Link to="/">{t("notFound.backHome")}</Link>
      </Button>
    </div>
  )
}
