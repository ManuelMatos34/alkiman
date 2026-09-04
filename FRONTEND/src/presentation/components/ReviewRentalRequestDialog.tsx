import { useEffect, useState } from "react"
import { useTranslation } from "react-i18next"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface ReviewRentalRequestDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** "approve" cambia el copy y el color del botón de confirmación; "reject" lo hace destructivo. */
  action: "approve" | "reject"
  description: string
  isPending: boolean
  onConfirm: (staffNote: string | null) => void
}

/** Diálogo de confirmación compartido para aprobar/rechazar un pedido de renta, con nota opcional. */
export function ReviewRentalRequestDialog({
  open,
  onOpenChange,
  action,
  description,
  isPending,
  onConfirm,
}: ReviewRentalRequestDialogProps) {
  const { t } = useTranslation(["rentalRequests", "common"])
  const [staffNote, setStaffNote] = useState("")

  useEffect(() => {
    if (open) setStaffNote("")
  }, [open])

  const isApprove = action === "approve"

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isApprove ? t("dialog.titleApprove") : t("dialog.titleReject")}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <div className="space-y-2">
          <Label htmlFor="staff-note">{t("dialog.noteLabel")}</Label>
          <Textarea
            id="staff-note"
            rows={3}
            placeholder={t("dialog.notePlaceholder")}
            value={staffNote}
            onChange={(e) => setStaffNote(e.target.value)}
          />
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            {t("common:buttons.cancel")}
          </Button>
          <Button
            type="button"
            variant={isApprove ? "default" : "destructive"}
            disabled={isPending}
            onClick={() => onConfirm(staffNote.trim() || null)}
          >
            {isPending
              ? t("common:status.saving")
              : isApprove
                ? t("dialog.confirmApprove")
                : t("dialog.confirmReject")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
