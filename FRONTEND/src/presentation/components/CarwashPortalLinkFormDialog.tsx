import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Dialog,
  DialogContent,
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
import { useCreatePortalLink } from "@/application/carwash/useCreatePortalLink"

function buildPortalLinkFormSchema(t: TFunction) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, t("portalLinks.validation.titleRequired"))
      .max(150, t("portalLinks.validation.titleMax")),
  })
}

type PortalLinkFormValues = z.infer<ReturnType<typeof buildPortalLinkFormSchema>>

interface CarwashPortalLinkFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Diálogo de creación de links del portal de Carwash. */
export function CarwashPortalLinkFormDialog({
  open,
  onOpenChange,
}: CarwashPortalLinkFormDialogProps) {
  const { t } = useTranslation(["carwash", "common"])
  const createPortalLink = useCreatePortalLink()

  const portalLinkFormSchema = useMemo(() => buildPortalLinkFormSchema(t), [t])

  const form = useForm<PortalLinkFormValues>({
    resolver: zodResolver(portalLinkFormSchema),
    defaultValues: { title: "" },
  })

  useEffect(() => {
    if (open) {
      form.reset({ title: "" })
    }
  }, [open, form])

  function onSubmit(values: PortalLinkFormValues) {
    createPortalLink.mutate(
      { title: values.title },
      {
        onSuccess: () => {
          toast.success(t("portalLinks.toast.createSuccess"))
          onOpenChange(false)
        },
        onError: () => toast.error(t("portalLinks.toast.createError")),
      }
    )
  }

  const isPending = createPortalLink.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("portalLinks.formDialog.titleNew")}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("portalLinks.formDialog.fields.title")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("portalLinks.formDialog.placeholders.title")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
