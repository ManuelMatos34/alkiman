import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Switch } from "@/components/ui/switch"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { useMe } from "@/application/me/useMe"
import { useUpdateMyTwoFactor } from "@/application/me/useUpdateMyTwoFactor"

/** Panel de seguridad: activar/desactivar autenticación de doble factor. */
export function SecuritySettingsPanel() {
  const { t } = useTranslation(["settingsForms", "common"])
  const { data: me } = useMe()
  const updateTwoFactor = useUpdateMyTwoFactor()
  const [confirmDisableOpen, setConfirmDisableOpen] = useState(false)

  function setTwoFactor(enabled: boolean) {
    updateTwoFactor.mutate(
      { enabled },
      {
        onSuccess: () =>
          toast.success(enabled ? t("security.toastEnabled") : t("security.toastDisabled")),
        onError: () => toast.error(t("security.toastError")),
      }
    )
  }

  function handleToggle(checked: boolean) {
    if (checked) {
      setTwoFactor(true)
      return
    }
    setConfirmDisableOpen(true)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("security.cardTitle")}</CardTitle>
        <CardDescription>
          {t("security.cardDescription")}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-between rounded-lg border border-border p-4">
          <div className="space-y-0.5">
            <p className="text-sm font-medium">{t("security.twoFactorLabel")}</p>
            <p className="text-sm text-muted-foreground">
              {t("security.twoFactorDescription")}
            </p>
          </div>
          <Switch
            checked={me?.twoFactorEnabled ?? false}
            disabled={!me || updateTwoFactor.isPending}
            onCheckedChange={handleToggle}
          />
        </div>
      </CardContent>

      <AlertDialog open={confirmDisableOpen} onOpenChange={setConfirmDisableOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("security.confirmDisableTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("security.confirmDisableDescription")}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("common:buttons.cancel")}</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => setTwoFactor(false)}
            >
              {t("security.disableButton")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  )
}
