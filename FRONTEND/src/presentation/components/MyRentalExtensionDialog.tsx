import { useEffect, useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
import { useCreateExtensionRequest } from "@/application/myRental/useCreateExtensionRequest"

function buildExtensionFormSchema(t: TFunction) {
  return z.object({
    requestedPeriods: z
      .string()
      .min(1, t("validation.extensionQuantityRequired"))
      .refine((val) => !Number.isNaN(Number(val)), t("validation.extensionQuantityInvalid"))
      .refine((val) => Number.isInteger(Number(val)), t("validation.extensionQuantityInteger"))
      .refine((val) => Number(val) >= 1, t("validation.extensionQuantityMin")),
  })
}

type ExtensionFormValues = z.infer<ReturnType<typeof buildExtensionFormSchema>>

function getErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

interface MyRentalExtensionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  token: string | undefined
  identifier: string
  onRequested: () => void
}

/** Diálogo público para que el cliente pida una prórroga sobre su renta activa. */
export function MyRentalExtensionDialog({
  open,
  onOpenChange,
  token,
  identifier,
  onRequested,
}: MyRentalExtensionDialogProps) {
  const { t } = useTranslation("myRental")
  const createExtensionRequest = useCreateExtensionRequest(token)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const extensionFormSchema = useMemo(() => buildExtensionFormSchema(t), [t])

  const form = useForm<ExtensionFormValues>({
    resolver: zodResolver(extensionFormSchema),
    defaultValues: { requestedPeriods: "1" },
  })

  useEffect(() => {
    if (open) {
      form.reset({ requestedPeriods: "1" })
      setSubmitError(null)
    }
  }, [open, form])

  async function onSubmit(values: ExtensionFormValues) {
    setSubmitError(null)
    try {
      await createExtensionRequest.mutateAsync({
        identifier,
        requestedPeriods: Number(values.requestedPeriods),
      })
      onRequested()
    } catch (error) {
      setSubmitError(getErrorMessage(error, t("errors.extensionFailed")))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("extension.title")}</DialogTitle>
          <DialogDescription>{t("extension.description")}</DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="requestedPeriods"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("extension.quantityLabel")}</FormLabel>
                  <FormControl>
                    <Input type="number" min={1} step={1} {...field} />
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
              <Button type="submit" disabled={createExtensionRequest.isPending}>
                {createExtensionRequest.isPending ? t("extension.sending") : t("extension.submit")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
