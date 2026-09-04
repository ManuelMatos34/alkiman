import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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
import { useCarwashServices } from "@/application/carwash/useCarwashServices"
import { useCarwashExtras } from "@/application/carwash/useCarwashExtras"
import { useRegisterTicket } from "@/application/carwash/useRegisterTicket"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

const CURRENT_YEAR = new Date().getFullYear()

function buildRegisterTicketFormSchema(t: TFunction) {
  return z.object({
    customerName: z
      .string()
      .trim()
      .min(1, t("board.registerDialog.validation.customerNameRequired"))
      .max(150, t("board.registerDialog.validation.customerNameMax")),
    customerPhone: z
      .string()
      .trim()
      .min(1, t("board.registerDialog.validation.customerPhoneRequired"))
      .max(30, t("board.registerDialog.validation.customerPhoneMax")),
    customerEmail: z
      .union([z.literal(""), z.string().trim().email(t("board.registerDialog.validation.customerEmailInvalid"))])
      .optional(),
    serviceId: z.string().min(1, t("board.registerDialog.validation.serviceRequired")),
    extraIds: z.array(z.number()),
    vehiclePlate: z
      .string()
      .trim()
      .min(1, t("board.registerDialog.validation.vehiclePlateRequired"))
      .max(20, t("board.registerDialog.validation.vehiclePlateMax")),
    vehicleBrand: z.string().trim().max(60, t("vehicle.validation.brandMax")).optional(),
    vehicleModel: z.string().trim().max(60, t("vehicle.validation.modelMax")).optional(),
    vehicleYear: z
      .union([
        z.literal(""),
        z
          .string()
          .regex(/^\d{4}$/, t("vehicle.validation.yearInvalid"))
          .refine(
            (value) => Number(value) >= 1900 && Number(value) <= CURRENT_YEAR + 1,
            t("vehicle.validation.yearRange", { min: 1900, max: CURRENT_YEAR + 1 })
          ),
      ])
      .optional(),
    vehicleColor: z.string().trim().max(40, t("vehicle.validation.colorMax")).optional(),
  })
}

type RegisterTicketFormValues = z.infer<ReturnType<typeof buildRegisterTicketFormSchema>>

const EMPTY_FORM: RegisterTicketFormValues = {
  customerName: "",
  customerPhone: "",
  customerEmail: "",
  serviceId: "",
  extraIds: [],
  vehiclePlate: "",
  vehicleBrand: "",
  vehicleModel: "",
  vehicleYear: "",
  vehicleColor: "",
}

interface CarwashRegisterTicketDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

/** Diálogo de registro presencial de un vehículo en la cola (lo abre el Encargado). */
export function CarwashRegisterTicketDialog({
  open,
  onOpenChange,
}: CarwashRegisterTicketDialogProps) {
  const { t, i18n } = useTranslation(["carwash", "common"])
  const { data: services } = useCarwashServices()
  const { data: extras } = useCarwashExtras()
  const activeServices = services?.filter((service) => service.isActive) ?? []
  const activeExtras = extras?.filter((extra) => extra.isActive) ?? []
  const registerTicket = useRegisterTicket()

  const registerTicketFormSchema = useMemo(() => buildRegisterTicketFormSchema(t), [t])

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), { style: "currency", currency: "DOP" }),
    [i18n.language]
  )

  const form = useForm<RegisterTicketFormValues>({
    resolver: zodResolver(registerTicketFormSchema),
    defaultValues: EMPTY_FORM,
  })

  useEffect(() => {
    if (open) form.reset(EMPTY_FORM)
  }, [open, form])

  // Precio en vivo, para que el Encargado le pueda decir el total al cliente antes de confirmar.
  const selectedServiceId = form.watch("serviceId")
  const selectedExtraIds = form.watch("extraIds")
  const total = useMemo(() => {
    const service = activeServices.find((s) => String(s.id) === selectedServiceId)
    const extrasTotal = activeExtras
      .filter((extra) => selectedExtraIds.includes(extra.id))
      .reduce((sum, extra) => sum + extra.price, 0)
    return (service?.price ?? 0) + extrasTotal
  }, [activeServices, activeExtras, selectedServiceId, selectedExtraIds])

  function onSubmit(values: RegisterTicketFormValues) {
    registerTicket.mutate(
      {
        customerName: values.customerName,
        customerPhone: values.customerPhone,
        customerEmail: values.customerEmail?.length ? values.customerEmail : null,
        serviceId: Number(values.serviceId),
        extraIds: values.extraIds,
        vehiclePlate: values.vehiclePlate,
        vehicleBrand: values.vehicleBrand?.length ? values.vehicleBrand : null,
        vehicleModel: values.vehicleModel?.length ? values.vehicleModel : null,
        vehicleYear: values.vehicleYear?.length ? Number(values.vehicleYear) : null,
        vehicleColor: values.vehicleColor?.length ? values.vehicleColor : null,
      },
      {
        onSuccess: () => {
          toast.success(t("board.registerDialog.toast.success"))
          onOpenChange(false)
        },
        onError: () => toast.error(t("board.registerDialog.toast.error")),
      }
    )
  }

  const isPending = registerTicket.isPending
  const hasServices = activeServices.length > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("board.registerDialog.title")}</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="customerName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("board.registerDialog.fields.customerName")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("board.registerDialog.fields.customerNamePlaceholder")}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="customerPhone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("board.registerDialog.fields.customerPhone")}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder={t("board.registerDialog.fields.customerPhonePlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="customerEmail"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>
                      {t("board.registerDialog.fields.customerEmail")}{" "}
                      <span className="text-muted-foreground">({t("common:labels.optional")})</span>
                    </FormLabel>
                    <FormControl>
                      <Input
                        type="email"
                        placeholder={t("board.registerDialog.fields.customerEmailPlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="space-y-4 rounded-lg border border-border/60 p-4">
              <p className="text-sm font-medium">{t("vehicle.sectionTitle")}</p>

              <FormField
                control={form.control}
                name="vehiclePlate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("board.registerDialog.fields.vehiclePlate")}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder={t("board.registerDialog.fields.vehiclePlatePlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  control={form.control}
                  name="vehicleBrand"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("vehicle.fields.brand")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("vehicle.placeholders.brand")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="vehicleModel"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("vehicle.fields.model")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("vehicle.placeholders.model")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="vehicleYear"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("vehicle.fields.year")}</FormLabel>
                      <FormControl>
                        <Input
                          inputMode="numeric"
                          placeholder={t("vehicle.placeholders.year")}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="vehicleColor"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("vehicle.fields.color")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("vehicle.placeholders.color")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </div>

            <FormField
              control={form.control}
              name="serviceId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("board.registerDialog.fields.service")}</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value} disabled={!hasServices}>
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue
                          placeholder={
                            hasServices
                              ? t("board.registerDialog.fields.servicePlaceholder")
                              : t("board.registerDialog.fields.serviceEmpty")
                          }
                        />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {activeServices.map((service) => (
                        <SelectItem key={service.id} value={String(service.id)}>
                          {service.name} — {currencyFormatter.format(service.price)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {activeExtras.length > 0 && (
              <FormField
                control={form.control}
                name="extraIds"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>
                      {t("extras.pickerLabel")}{" "}
                      <span className="text-muted-foreground">({t("common:labels.optional")})</span>
                    </FormLabel>
                    <div className="space-y-2 rounded-lg border border-border/60 p-3">
                      {activeExtras.map((extra) => {
                        const checked = field.value.includes(extra.id)
                        return (
                          <label
                            key={extra.id}
                            className="flex cursor-pointer items-center justify-between gap-3 text-sm"
                          >
                            <span className="flex items-center gap-2">
                              <Checkbox
                                checked={checked}
                                onCheckedChange={(value) =>
                                  field.onChange(
                                    value
                                      ? [...field.value, extra.id]
                                      : field.value.filter((id) => id !== extra.id)
                                  )
                                }
                              />
                              {extra.name}
                            </span>
                            <span className="text-muted-foreground">
                              {currencyFormatter.format(extra.price)}
                            </span>
                          </label>
                        )
                      })}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}

            <div className="flex items-center justify-between rounded-lg bg-muted/60 px-4 py-3 text-sm">
              <span className="font-medium">{t("extras.total")}</span>
              <span className="text-base font-semibold">{currencyFormatter.format(total)}</span>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending || !hasServices}>
                {isPending ? t("common:status.saving") : t("board.registerDialog.submit")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
