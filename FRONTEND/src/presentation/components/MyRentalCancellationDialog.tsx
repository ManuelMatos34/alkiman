import { useEffect, useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useCreateCancellationRequest } from "@/application/myRental/useCreateCancellationRequest"

function buildCancellationFormSchema(t: TFunction) {
  return z.object({
    reason: z
      .string()
      .trim()
      .min(1, t("validation.cancellationReasonRequired"))
      .max(500, t("validation.cancellationReasonMax")),
  })
}

type CancellationFormValues = z.infer<ReturnType<typeof buildCancellationFormSchema>>

function getErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

interface MyRentalCancellationDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  token: string | undefined
  identifier: string
  onRequested: () => void
}

/** Diálogo público para que el cliente pida la cancelación anticipada de su renta. */
export function MyRentalCancellationDialog({
  open,
  onOpenChange,
  token,
  identifier,
  onRequested,
}: MyRentalCancellationDialogProps) {
  const { t } = useTranslation("myRental")
  const createCancellationRequest = useCreateCancellationRequest(token)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const cancellationFormSchema = useMemo(() => buildCancellationFormSchema(t), [t])

  const form = useForm<CancellationFormValues>({
    resolver: zodResolver(cancellationFormSchema),
    defaultValues: { reason: "" },
  })

  useEffect(() => {
    if (open) {
      form.reset({ reason: "" })
      setSubmitError(null)
    }
  }, [open, form])

  async function onSubmit(values: CancellationFormValues) {
    setSubmitError(null)
    try {
      await createCancellationRequest.mutateAsync({ identifier, reason: values.reason })
      onRequested()
    } catch (error) {
      setSubmitError(getErrorMessage(error, t("errors.cancellationFailed")))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("cancellation.title")}</DialogTitle>
          <DialogDescription>{t("cancellation.description")}</DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("cancellation.reasonLabel")}</FormLabel>
                  <FormControl>
                    <Textarea rows={4} placeholder={t("cancellation.reasonPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {submitError && <p className="text-sm text-destructive">{submitError}</p>}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" variant="destructive" disabled={createCancellationRequest.isPending}>
                {createCancellationRequest.isPending ? t("cancellation.sending") : t("cancellation.submit")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
