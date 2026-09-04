import { useMemo } from "react"
import { Link, useParams } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { ImageOff, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { usePortalCatalog } from "@/application/portal/usePortalCatalog"
import { rentalTypeLabels } from "@/domain/types/asset"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

/** Catálogo público del Portal de Rentas: primer paso de la pasarela, el cliente elige el activo que quiere rentar. */
export function PortalCatalogPage() {
  const { t, i18n } = useTranslation("portal")
  const { slug } = useParams<{ slug: string }>()
  const { data: catalog, isLoading, isError } = usePortalCatalog(slug)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError || !catalog) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background text-center px-4">
        <h1 className="text-2xl font-semibold tracking-tight">{t("catalog.unavailable.title")}</h1>
        <p className="text-sm text-muted-foreground">{t("catalog.unavailable.description")}</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <PublicAppearanceSync themeMode={catalog.themeMode} accentColor={catalog.accentColor} />
      <header className="border-b border-border/60 bg-card">
        <div className="mx-auto max-w-6xl px-4 py-8">
          <p className="text-sm font-medium text-muted-foreground">{catalog.businessName}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{catalog.linkTitle}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("catalog.subtitle")}</p>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">
        {catalog.assets.length === 0 && (
          <p className="py-12 text-center text-muted-foreground">{t("catalog.empty")}</p>
        )}

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {catalog.assets.map((asset) => (
            <Card key={asset.id} className="overflow-hidden py-0">
              <div className="flex h-40 items-center justify-center bg-muted">
                {asset.imageUrl ? (
                  <img
                    src={asset.imageUrl}
                    alt={asset.name}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <ImageOff className="h-8 w-8 text-muted-foreground" />
                )}
              </div>
              <CardHeader className="pt-6">
                <CardTitle className="flex items-start justify-between gap-2">
                  <span>{asset.name}</span>
                </CardTitle>
                <div className="flex flex-wrap gap-1.5">
                  <Badge variant="secondary">{asset.categoryName}</Badge>
                  <Badge variant="outline">
                    {rentalTypeLabels[asset.rentalType] ?? asset.rentalType}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <p className="line-clamp-3 text-sm text-muted-foreground">
                  {asset.description ?? t("catalog.noDescription")}
                </p>
              </CardContent>
              <CardFooter className="flex items-center justify-between gap-2 border-t pt-6">
                <span className="text-lg font-semibold">
                  {currencyFormatter.format(asset.basePrice)}
                </span>
                <Button asChild size="sm">
                  <Link to={`/p/${slug}/rentar/${asset.id}`}>{t("catalog.rentButton")}</Link>
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </main>
    </div>
  )
}
