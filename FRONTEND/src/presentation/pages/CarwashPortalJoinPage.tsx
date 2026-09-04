import { useMemo } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { Checkbox } from "@/components/ui/checkbox"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useCarwashPublicLink } from "@/application/carwash/useCarwashPublicLink"
import { useJoinCarwashQueue } from "@/application/carwash/useJoinCarwashQueue"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"

const CURRENT_YEAR = new Date().getFullYear()

function buildJoinFormSchema(t: TFunction) {
  return z.object({
    customerName: z.string().trim().min(1, t("join.validation.customerNameRequired")),
    customerPhone: z.string().trim().min(1, t("join.validation.customerPhoneRequired")),
    customerEmail: z
      .union([z.literal(""), z.string().trim().email(t("join.validation.customerEmailInvalid"))])
      .optional(),
    serviceId: z.string().min(1, t("join.validation.serviceRequired")),
    extraIds: z.array(z.number()),
    vehiclePlate: z.string().trim().min(1, t("join.validation.vehiclePlateRequired")),
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

type JoinFormValues = z.infer<ReturnType<typeof buildJoinFormSchema>>

const EMPTY_FORM: JoinFormValues = {
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

/** Link público de Carwash: el cliente se auto-registra en la cola sin estar físicamente presente. */
export function CarwashPortalJoinPage() {
  const { t, i18n } = useTranslation("carwash")
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const { data: link, isLoading, isError } = useCarwashPublicLink(slug)
  const joinQueue = useJoinCarwashQueue(slug)

  const joinFormSchema = useMemo(() => buildJoinFormSchema(t), [t])

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), { style: "currency", currency: "DOP" }),
    [i18n.language]
  )

  const form = useForm<JoinFormValues>({
    resolver: zodResolver(joinFormSchema),
    defaultValues: EMPTY_FORM,
  })

  // Precio en vivo: el cliente ve cuánto va a pagar antes de tomar el turno.
  const selectedServiceId = form.watch("serviceId")
  const selectedExtraIds = form.watch("extraIds")
  const total = useMemo(() => {
    const service = link?.services.find((s) => String(s.id) === selectedServiceId)
    const extrasTotal = (link?.extras ?? [])
      .filter((extra) => selectedExtraIds.includes(extra.id))
      .reduce((sum, extra) => sum + extra.price, 0)
    return (service?.price ?? 0) + extrasTotal
  }, [link, selectedServiceId, selectedExtraIds])

  function onSubmit(values: JoinFormValues) {
    joinQueue.mutate(
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
        onSuccess: (ticket) => {
          navigate(`/lavado/turno/${ticket.accessToken}`)
        },
        onError: () => toast.error(t("join.toast.error")),
      }
    )
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError || !link) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("join.unavailable.title")}</h1>
        <p className="text-sm text-muted-foreground">{t("join.unavailable.description")}</p>
      </div>
    )
  }

  const hasServices = link.services.length > 0
  const isPending = joinQueue.isPending

  return (
    <div className="min-h-screen bg-background px-4 py-10">
      <PublicAppearanceSync themeMode={link.themeMode} accentColor={link.accentColor} />
      <div className="mx-auto max-w-lg space-y-4">
        <div className="text-center">
          <p className="text-sm font-medium text-muted-foreground">{link.businessName}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{link.linkTitle}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("join.subtitle")}</p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t("join.formTitle")}</CardTitle>
          </CardHeader>
          <CardContent>
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="customerName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("join.fields.customerName")}</FormLabel>
                      <FormControl>
                        <Input placeholder={t("join.fields.customerNamePlaceholder")} {...field} />
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
                        <FormLabel>{t("join.fields.customerPhone")}</FormLabel>
                        <FormControl>
                          <Input placeholder={t("join.fields.customerPhonePlaceholder")} {...field} />
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
                          {t("join.fields.customerEmail")}{" "}
                          <span className="text-muted-foreground">({t("common:labels.optional")})</span>
                        </FormLabel>
                        <FormControl>
                          <Input
                            type="email"
                            placeholder={t("join.fields.customerEmailPlaceholder")}
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="serviceId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("join.fields.service")}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value} disabled={!hasServices}>
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue
                              placeholder={
                                hasServices
                                  ? t("join.fields.servicePlaceholder")
                                  : t("join.fields.serviceEmpty")
                              }
                            />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {link.services.map((service) => (
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

                {link.extras.length > 0 && (
                  <FormField
                    control={form.control}
                    name="extraIds"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>
                          {t("extras.pickerLabel")}{" "}
                          <span className="text-muted-foreground">
                            ({t("common:labels.optional")})
                          </span>
                        </FormLabel>
                        <div className="space-y-2 rounded-lg border border-border/60 p-3">
                          {link.extras.map((extra) => (
                            <label
                              key={extra.id}
                              className="flex cursor-pointer items-center justify-between gap-3 text-sm"
                            >
                              <span className="flex items-center gap-2">
                                <Checkbox
                                  checked={field.value.includes(extra.id)}
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
                          ))}
                        </div>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                )}

                <div className="space-y-4 rounded-lg border border-border/60 p-4">
                  <p className="text-sm font-medium">{t("vehicle.sectionTitle")}</p>

                  <FormField
                    control={form.control}
                    name="vehiclePlate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t("join.fields.vehiclePlate")}</FormLabel>
                        <FormControl>
                          <Input placeholder={t("join.fields.vehiclePlatePlaceholder")} {...field} />
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

                <div className="flex items-center justify-between rounded-lg bg-muted/60 px-4 py-3 text-sm">
                  <span className="font-medium">{t("extras.total")}</span>
                  <span className="text-base font-semibold">
                    {currencyFormatter.format(total)}
                  </span>
                </div>

                <Button type="submit" className="w-full" disabled={isPending || !hasServices}>
                  {isPending ? t("join.submitting") : t("join.submit")}
                </Button>
              </form>
            </Form>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
