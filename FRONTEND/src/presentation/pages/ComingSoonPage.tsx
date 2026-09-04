import { Link } from "react-router-dom"
import { ArrowLeft } from "lucide-react"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"

export function ComingSoonPage({ title }: { title: string }) {
  const { t } = useTranslation("misc")
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {t("comingSoon.description", { title: title.toLowerCase() })}
        </p>
      </div>
      <Button variant="outline" size="sm" asChild>
        <Link to="/">
          <ArrowLeft className="h-4 w-4" />
          {t("comingSoon.backToDashboard")}
        </Link>
      </Button>
    </div>
  )
}
