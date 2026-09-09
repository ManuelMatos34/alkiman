import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
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
import { useCreateBarbershopStylist, useUpdateBarbershopStylist } from "@/application/barbershop/useBarbershopStylists"
import type { BarbershopStylist } from "@/domain/types/barbershop"

function buildSchema(t: TFunction) {
  return z.object({
    fullName: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(150, t("validation.nameMax")),
    phone: z.string().trim().optional(),
    email: z
      .string()
      .trim()
      .email(t("common:validation.emailInvalid"))
      .optional()
      .or(z.literal("")),
    isActive: z.boolean(),
  })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  stylist?: BarbershopStylist | null
}

export function BarbershopStylistFormDialog({ open, onOpenChange, stylist }: Props) {
  const { t } = useTranslation(["barbershop", "common"])
  const isEditing = !!stylist
  const create = useCreateBarbershopStylist()
  const update = useUpdateBarbershopStylist()

  const schema = useMemo(() => buildSchema(t), [t])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: "", phone: "", email: "", isActive: true },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        fullName: stylist?.fullName ?? "",
        phone: stylist?.phone ?? "",
        email: stylist?.email ?? "",
        isActive: stylist?.isActive ?? true,
      })
    }
  }, [open, stylist, form])

  function onSubmit(values: FormValues) {
    const common = {
      fullName: values.fullName,
      phone: values.phone?.length ? values.phone : null,
      email: values.email?.length ? values.email : null,
    }

    if (stylist) {
      update.mutate(
        { id: stylist.id, request: { ...common, isActive: values.isActive } },
        {
          onSuccess: () => { toast.success(t("toast.updateSuccess")); onOpenChange(false) },
          onError: () => toast.error(t("toast.updateError")),
        }
      )
      return
    }

    create.mutate(common, {
      onSuccess: () => { toast.success(t("toast.createSuccess")); onOpenChange(false) },
      onError: () => toast.error(t("toast.createError")),
    })
  }

  const isPending = create.isPending || update.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.stylist.titleEdit") : t("formDialog.stylist.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="fullName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.stylist.fields.fullName")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.stylist.placeholders.fullName")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="phone"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.stylist.fields.phone")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.stylist.placeholders.phone")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.stylist.fields.email")}</FormLabel>
                  <FormControl>
                    <Input
                      type="email"
                      placeholder={t("formDialog.stylist.placeholders.email")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isEditing && (
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border border-border/60 p-3">
                    <FormLabel className="mb-0">{t("formDialog.stylist.fields.active")}</FormLabel>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                  </FormItem>
                )}
              />
            )}

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
