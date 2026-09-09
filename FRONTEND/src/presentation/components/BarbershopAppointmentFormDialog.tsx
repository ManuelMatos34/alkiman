import { useEffect, useMemo } from "react"
import { useForm, useWatch } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
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
import { useCreateBarbershopAppointment } from "@/application/barbershop/useBarbershopAppointments"
import { useBarbershopServices } from "@/application/barbershop/useBarbershopServices"
import { useBarbershopStylists } from "@/application/barbershop/useBarbershopStylists"
import { TimeSlotPicker } from "@/presentation/components/TimeSlotPicker"

const NO_VALUE = "__none__"

function buildSchema(t: TFunction) {
  return z.object({
    clientName: z.string().trim().min(1, t("validation.clientNameRequired")),
    clientPhone: z.string().trim().optional(),
    clientEmail: z
      .string()
      .trim()
      .email(t("common:validation.emailInvalid"))
      .optional()
      .or(z.literal("")),
    serviceId: z.string().optional(),
    stylistId: z.string().optional(),
    scheduledAt: z.string().min(1, t("validation.scheduledAtRequired")),
    notes: z.string().trim().optional(),
  })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  defaultDate?: string
}

export function BarbershopAppointmentFormDialog({ open, onOpenChange, defaultDate }: Props) {
  const { t } = useTranslation(["barbershop", "common"])
  const create = useCreateBarbershopAppointment()
  const { data: services } = useBarbershopServices()
  const { data: stylists } = useBarbershopStylists()

  const schema = useMemo(() => buildSchema(t), [t])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      clientName: "",
      clientPhone: "",
      clientEmail: "",
      serviceId: NO_VALUE,
      stylistId: NO_VALUE,
      scheduledAt: "",
      notes: "",
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        clientName: "",
        clientPhone: "",
        clientEmail: "",
        serviceId: NO_VALUE,
        stylistId: NO_VALUE,
      scheduledAt: "",
        notes: "",
      })
    }
  }, [open, defaultDate, form])

  const watchedServiceId = useWatch({ control: form.control, name: "serviceId" })
  const numericServiceId =
    watchedServiceId && watchedServiceId !== NO_VALUE ? Number(watchedServiceId) : null

  function onSubmit(values: FormValues) {
    create.mutate(
      {
        clientName: values.clientName,
        clientPhone: values.clientPhone?.length ? values.clientPhone : null,
        clientEmail: values.clientEmail?.length ? values.clientEmail : null,
        serviceId: values.serviceId && values.serviceId !== NO_VALUE ? Number(values.serviceId) : null,
        stylistId: values.stylistId && values.stylistId !== NO_VALUE ? values.stylistId : null,
        scheduledAt: values.scheduledAt,
        notes: values.notes?.length ? values.notes : null,
      },
      {
        onSuccess: () => { toast.success(t("toast.createSuccess")); onOpenChange(false) },
        onError: () => toast.error(t("toast.createError")),
      }
    )
  }

  const isPending = create.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("formDialog.appointment.titleNew")}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="clientName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.appointment.fields.clientName")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.appointment.placeholders.clientName")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="clientPhone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.appointment.fields.clientPhone")}</FormLabel>
                    <FormControl>
                      <Input placeholder={t("formDialog.appointment.placeholders.clientPhone")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="clientEmail"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.appointment.fields.clientEmail")}</FormLabel>
                    <FormControl>
                      <Input type="email" placeholder={t("formDialog.appointment.placeholders.clientEmail")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="serviceId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.appointment.fields.service")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value={NO_VALUE}>
                          {t("formDialog.appointment.placeholders.serviceNone")}
                        </SelectItem>
                        {services?.filter((s) => s.isActive).map((s) => (
                          <SelectItem key={s.id} value={String(s.id)}>
                            {s.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="stylistId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("formDialog.appointment.fields.stylist")}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value={NO_VALUE}>
                          {t("formDialog.appointment.placeholders.stylistNone")}
                        </SelectItem>
                        {stylists?.filter((s) => s.isActive).map((s) => (
                          <SelectItem key={s.id} value={s.id}>
                            {s.fullName}
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
              name="scheduledAt"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.appointment.fields.scheduledAt")}</FormLabel>
                  <FormControl>
                    <TimeSlotPicker
                      key={open ? "open" : "closed"}
                      mode="internal"
                      value={field.value}
                      onChange={field.onChange}
                      serviceId={numericServiceId}
                      disabled={isPending}
                      defaultDate={defaultDate}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="notes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.appointment.fields.notes")}</FormLabel>
                  <FormControl>
                    <Textarea placeholder={t("formDialog.appointment.placeholders.notes")} {...field} />
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
