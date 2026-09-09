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
import { useCreateService } from "@/application/carwash/useCreateService"
import { useUpdateService } from "@/application/carwash/useUpdateService"
import { buildDurationOptions } from "@/lib/carwashDuration"
import type { CarwashServiceItem } from "@/domain/types/carwash"

function buildServiceFormSchema(t: TFunction) {
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
    estimatedMinutes: z
      .string()
      .min(1, t("validation.minutesRequired"))
      .refine((val) => !Number.isNaN(Number(val)), t("validation.minutesInvalid"))
      .refine((val) => Number.isInteger(Number(val)), t("validation.minutesInteger"))
      .refine((val) => Number(val) > 0, t("validation.minutesPositive")),
    isActive: z.boolean(),
  })
}

type ServiceFormValues = z.infer<ReturnType<typeof buildServiceFormSchema>>

interface CarwashServiceFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  service?: CarwashServiceItem | null
}

/** Diálogo de creación/edición de servicios de Carwash. */
export function CarwashServiceFormDialog({
  open,
  onOpenChange,
  service,
}: CarwashServiceFormDialogProps) {
  const { t } = useTranslation(["carwash", "common"])
  const isEditing = !!service
  const createService = useCreateService()
  const updateService = useUpdateService()

  const serviceFormSchema = useMemo(() => buildServiceFormSchema(t), [t])

  // Sin 0: el servicio base define la estimación del turno, y uno de 0 minutos
  // dejaría la cola del portal sin tiempo de espera que mostrar.
  const savedMinutes = service?.estimatedMinutes
  const durationOptions = useMemo(
    () => buildDurationOptions(savedMinutes, { allowZero: false }),
    [savedMinutes]
  )

  const form = useForm<ServiceFormValues>({
    resolver: zodResolver(serviceFormSchema),
    defaultValues: {
      name: "",
      description: "",
      price: "0",
      estimatedMinutes: "30",
      isActive: true,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        name: service?.name ?? "",
        description: service?.description ?? "",
        price: service ? String(service.price) : "0",
        estimatedMinutes: service ? String(service.estimatedMinutes) : "30",
        isActive: service?.isActive ?? true,
      })
    }
  }, [open, service, form])

  function onSubmit(values: ServiceFormValues) {
    const common = {
      name: values.name,
      description: values.description?.length ? values.description : null,
      price: Number(values.price),
      estimatedMinutes: Number(values.estimatedMinutes),
    }

    if (service) {
      updateService.mutate(
        { id: service.id, request: { ...common, isActive: values.isActive } },
        {
          onSuccess: () => {
            toast.success(t("services.toast.updated"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("services.toast.updateError")),
        }
      )
      return
    }

    createService.mutate(common, {
      onSuccess: () => {
        toast.success(t("services.toast.created"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("services.toast.createError")),
    })
  }

  const isPending = createService.isPending || updateService.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("services.dialog.editTitle") : t("services.dialog.createTitle")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("services.dialog.fields.name")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("services.dialog.fields.namePlaceholder")} {...field} />
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
                    <FormLabel>{t("services.dialog.fields.price")}</FormLabel>
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
                    <FormLabel>{t("services.dialog.fields.estimatedMinutes")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {durationOptions.map((minutes) => (
                          <SelectItem key={minutes} value={String(minutes)}>
                            {t("services.dialog.fields.durationMinutes", { minutes })}
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
                  <FormLabel>{t("services.dialog.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t("services.dialog.fields.descriptionPlaceholder")}
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
                    <FormLabel className="mb-0">{t("services.dialog.fields.active")}</FormLabel>
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
