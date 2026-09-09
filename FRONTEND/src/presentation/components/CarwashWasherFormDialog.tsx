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
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useCreateWasher } from "@/application/carwash/useCreateWasher"
import { useUpdateWasher } from "@/application/carwash/useUpdateWasher"
import type { CarwashWasher } from "@/domain/types/carwash"

function buildWasherFormSchema(t: TFunction) {
  return z.object({
    fullName: z
      .string()
      .trim()
      .min(1, t("washers.validation.nameRequired"))
      .max(150, t("washers.validation.nameMax")),
    phone: z.string().trim().max(30, t("washers.validation.phoneMax")).optional(),
    email: z.string().trim().email(t("washers.validation.emailInvalid")).optional().or(z.literal("")),
    isActive: z.boolean(),
  })
}

type WasherFormValues = z.infer<ReturnType<typeof buildWasherFormSchema>>

interface CarwashWasherFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  washer?: CarwashWasher | null
}

/**
 * Alta/edición de un lavador.
 *
 * Sólo pide nombre, teléfono, email y estado: la ficha del lavador es personal del
 * módulo, no una identidad del sistema, y no se vincula con ninguna cuenta.
 * Quien además deba iniciar sesión se crea aparte, en Usuarios del sistema.
 * El email es opcional pero, si se carga, el lavador recibe una notificación
 * cada vez que le asignan una propina.
 */
export function CarwashWasherFormDialog({
  open,
  onOpenChange,
  washer,
}: CarwashWasherFormDialogProps) {
  const { t } = useTranslation(["carwash", "common"])
  const isEditing = !!washer
  const createWasher = useCreateWasher()
  const updateWasher = useUpdateWasher()

  const washerFormSchema = useMemo(() => buildWasherFormSchema(t), [t])

  const form = useForm<WasherFormValues>({
    resolver: zodResolver(washerFormSchema),
    defaultValues: {
      fullName: "",
      phone: "",
      email: "",
      isActive: true,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        fullName: washer?.fullName ?? "",
        phone: washer?.phone ?? "",
        email: washer?.email ?? "",
        isActive: washer?.isActive ?? true,
      })
    }
  }, [open, washer, form])

  function onSubmit(values: WasherFormValues) {
    const common = {
      fullName: values.fullName,
      phone: values.phone?.length ? values.phone : null,
      email: values.email?.length ? values.email : null,
    }

    if (washer) {
      updateWasher.mutate(
        { id: washer.id, request: { ...common, isActive: values.isActive } },
        {
          onSuccess: () => {
            toast.success(t("washers.toast.updated"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("washers.toast.updateError")),
        }
      )
      return
    }

    createWasher.mutate(common, {
      onSuccess: () => {
        toast.success(t("washers.toast.created"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("washers.toast.createError")),
    })
  }

  const isPending = createWasher.isPending || updateWasher.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("washers.dialog.editTitle") : t("washers.dialog.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="fullName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("washers.dialog.fields.fullName")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("washers.dialog.fields.fullNamePlaceholder")}
                      {...field}
                    />
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
                  <FormLabel>{t("washers.dialog.fields.phone")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("washers.dialog.fields.phonePlaceholder")} {...field} />
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
                  <FormLabel>{t("washers.dialog.fields.email")}</FormLabel>
                  <FormControl>
                    <Input
                      type="email"
                      placeholder={t("washers.dialog.fields.emailPlaceholder")}
                      {...field}
                    />
                  </FormControl>
                  <FormDescription>{t("washers.dialog.fields.emailHint")}</FormDescription>
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
                    <div>
                      <FormLabel className="mb-0">{t("washers.dialog.fields.active")}</FormLabel>
                      <FormDescription>{t("washers.dialog.fields.activeHint")}</FormDescription>
                    </div>
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
