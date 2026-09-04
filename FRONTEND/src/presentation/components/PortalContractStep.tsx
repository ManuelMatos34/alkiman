import { useEffect } from "react"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { usePortalContractPreview } from "@/application/portal/usePortalContractPreview"
import type { PortalContractPreviewRequest } from "@/domain/types/portalContractPreview"

function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

interface PortalContractStepProps {
  slug: string | undefined
  request: PortalContractPreviewRequest
  accepted: boolean
  onAcceptedChange: (accepted: boolean) => void
}

/**
 * Paso 2 del wizard del Portal: previsualización del texto del contrato antes de firmarlo y
 * pagarlo. Vuelve a pedir el texto cada vez que cambian los datos de la renta (periods/quantity)
 * o los datos personales, ya que el contenido del contrato depende de ellos.
 */
export function PortalContractStep({
  slug,
  request,
  accepted,
  onAcceptedChange,
}: PortalContractStepProps) {
  const { t } = useTranslation("portal")
  const { data, isPending, isError, error, mutate } = usePortalContractPreview(slug)

  useEffect(() => {
    mutate(request)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    slug,
    request.assetId,
    request.fullName,
    request.identityNumber,
    request.email,
    request.phone,
    request.address,
    request.startDate,
    request.periods,
    request.quantity,
  ])

  if (isPending) {
    return (
      <div className="flex justify-center py-8">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="space-y-3 py-4 text-center">
        <p className="text-sm text-destructive">
          {extractErrorMessage(error, t("checkout.contract.errorText"))}
        </p>
        <Button type="button" variant="outline" size="sm" onClick={() => mutate(request)}>
          {t("checkout.contract.retry")}
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">{t("checkout.contract.info")}</p>
      {data && (
        <div className="max-h-72 overflow-y-auto whitespace-pre-wrap rounded-lg border border-border/60 bg-muted/40 p-4 text-sm">
          {data.content}
        </div>
      )}
      <label className="flex items-start gap-2 text-sm">
        <Checkbox
          checked={accepted}
          onCheckedChange={(checked) => onAcceptedChange(checked === true)}
        />
        <span>{t("checkout.contract.acceptLabel")}</span>
      </label>
    </div>
  )
}
