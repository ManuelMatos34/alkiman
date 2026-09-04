import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { SignaturePad } from "@/presentation/components/SignaturePad"
import { useCurrentLandlord } from "@/application/landlords/useCurrentLandlord"
import { useUpdateSignature } from "@/application/landlords/useUpdateSignature"

/** Firma digital de la empresa: se dibuja una única vez y se reutiliza en el PDF de todos los contratos junto a la firma del cliente. */
export function SignatureSettingsForm() {
  const { t } = useTranslation("settingsForms")
  const { data: landlord } = useCurrentLandlord()
  const updateSignature = useUpdateSignature()

  const [isDrawing, setIsDrawing] = useState(false)

  const hasSavedSignature = !!landlord?.signatureBase64
  const showPad = isDrawing || !hasSavedSignature

  function handleSave(base64Png: string) {
    updateSignature.mutate(base64Png, {
      onSuccess: () => {
        toast.success(t("signature.toastSuccess"))
        setIsDrawing(false)
      },
      onError: () => toast.error(t("signature.toastError")),
    })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("signature.cardTitle")}</CardTitle>
        <CardDescription>{t("signature.cardDescription")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {showPad ? (
          <SignaturePad onSave={handleSave} />
        ) : (
          <div className="max-w-sm rounded-lg border border-border/60 bg-muted/40 p-3">
            <img
              src={`data:image/png;base64,${landlord?.signatureBase64}`}
              alt={t("signature.previewAlt")}
              className="h-32 w-full object-contain"
            />
          </div>
        )}
      </CardContent>
      {!showPad && (
        <CardFooter className="justify-end">
          <Button type="button" variant="outline" onClick={() => setIsDrawing(true)}>
            {t("signature.changeButton")}
          </Button>
        </CardFooter>
      )}
    </Card>
  )
}
