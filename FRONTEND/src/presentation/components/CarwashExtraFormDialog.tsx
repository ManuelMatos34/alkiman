import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useCreateExtra } from "@/application/carwash/useCreateExtra"
import { useUpdateExtra } from "@/application/carwash/useUpdateExtra"
import { buildDurationOptions } from "@/lib/carwashDuration"
import type { CarwashExtraItem } from "@/domain/types/carwash"

function buildExtraFormSchema(t: TFunction) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, t("validation.nameRequired"))
      .max(100, t("validation.nameMax")),
    description: z.string().trim().max(300, t("validation.descriptionMax")).optional(),
    price: z
      .string()
      .min(1, t("validation.priceRequired"))
      .refine((val) => !Number.isNaN(Number(val)), t("validation.priceInvalid"))
      .refine((val) => Number(val) >= 0, t("validation.priceNegative")),
    // A diferencia del servicio base, un extra puede no sumar tiempo (ej: aromatizante).
    estimatedMinutes: z
      .string()
      .min(1, t("validation.minutesRequired"))
      .refine((val) => !Number.isNaN(Number(val)), t("validation.minutesInvalid"))
      .refine((val) => Number.isInteger(Number(val)), t("validation.minutesInteger"))
      .refine((val) => Number(val) >= 0, t("validation.minutesNonNegative")),
    isActive: z.boolean(),
  })
}

type ExtraFormValues = z.infer<ReturnType<typeof buildExtraFormSchema>>

interface CarwashExtraFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  extra?: CarwashExtraItem | null
}

/** Diálogo de creación/edición de agregados de Carwash (encerado, ozono, ...). */
export function CarwashExtraFormDialog({
  open,
  onOpenChange,
  extra,
}: CarwashExtraFormDialogProps) {
  const { t } = useTranslation(["carwash", "common"])
  const isEditing = !!extra
  const createExtra = useCreateExtra()
  const updateExtra = useUpdateExtra()

  const extraFormSchema = useMemo(() => buildExtraFormSchema(t), [t])

  // Un agregado puede no sumar tiempo a la cola, así que acá el 0 sí se ofrece.
  const savedMinutes = extra?.estimatedMinutes
  const durationOptions = useMemo(
    () => buildDurationOptions(savedMinutes, { allowZero: true }),
    [savedMinutes]
  )

  const form = useForm<ExtraFormValues>({
    resolver: zodResolver(extraFormSchema),
    defaultValues: {
      name: "",
      description: "",
      price: "0",
      estimatedMinutes: "10",
      isActive: true,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        name: extra?.name ?? "",
        description: extra?.description ?? "",
        price: extra ? String(extra.price) : "0",
        estimatedMinutes: extra ? String(extra.estimatedMinutes) : "10",
        isActive: extra?.isActive ?? true,
      })
    }
  }, [open, extra, form])

  function onSubmit(values: ExtraFormValues) {
    const common = {
      name: values.name,
      description: values.description?.length ? values.description : null,
      price: Number(values.price),
      estimatedMinutes: Number(values.estimatedMinutes),
    }

    if (extra) {
      updateExtra.mutate(
        { id: extra.id, request: { ...common, isActive: values.isActive } },
        {
          onSuccess: () => {
            toast.success(t("extras.toast.updated"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("extras.toast.updateError")),
        }
      )
      return
    }

    createExtra.mutate(common, {
      onSuccess: () => {
        toast.success(t("extras.toast.created"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("extras.toast.createError")),
    })
  }

  const isPending = createExtra.isPending || updateExtra.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("extras.dialog.editTitle") : t("extras.dialog.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("extras.dialog.fields.name")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("extras.dialog.fields.namePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="price"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("extras.dialog.fields.price")}</FormLabel>
                    <FormControl>
                      <Input type="number" step="0.01" min="0" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="estimatedMinutes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("extras.dialog.fields.estimatedMinutes")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {durationOptions.map((minutes) => (
                          <SelectItem key={minutes} value={String(minutes)}>
                            {minutes === 0
                              ? t("extras.dialog.fields.durationNone")
                              : t("extras.dialog.fields.durationMinutes", { minutes })}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("extras.dialog.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t("extras.dialog.fields.descriptionPlaceholder")}
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
                    <FormLabel className="mb-0">{t("extras.dialog.fields.active")}</FormLabel>
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
