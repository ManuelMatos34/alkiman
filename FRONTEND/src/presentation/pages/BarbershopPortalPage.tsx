import { useMemo, useState } from "react"
import { Link, useParams } from "react-router-dom"
import { useForm, useWatch } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent } from "@/components/ui/card"
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
import { toast } from "sonner"
import { useBarbershopPublicLink, useBookBarbershopAppointment } from "@/application/barbershop/useBarbershopPublic"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { TimeSlotPicker } from "@/presentation/components/TimeSlotPicker"
import type { BarbershopBookingResponse } from "@/domain/types/barbershop"

const NO_VALUE = "__none__"

function buildSchema(t: TFunction) {
  return z.object({
    clientName: z.string().trim().min(1, t("validation.clientNameRequired")),
    clientPhone: z.string().trim().optional(),
    clientEmail: z
      .string()
      .trim()
      .email()
      .optional()
      .or(z.literal("")),
    serviceId: z.string().optional(),
    scheduledAt: z.string().min(1, t("validation.scheduledAtRequired")),
    notes: z.string().trim().optional(),
  })
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>

export function BarbershopPortalPage() {
  const { t, i18n } = useTranslation("barbershop")
  const { slug } = useParams<{ slug: string }>()

  const { data: link, isLoading, isError } = useBarbershopPublicLink(slug)
  const bookAppointment = useBookBarbershopAppointment(slug)

  const [booking, setBooking] = useState<BarbershopBookingResponse | null>(null)

  const schema = useMemo(() => buildSchema(t), [t])

  const dateTimeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        dateStyle: "long",
        timeStyle: "short",
      }),
    [i18n.language]
  )

  const priceFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "decimal",
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }),
    [i18n.language]
  )

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      clientName: "",
      clientPhone: "",
      clientEmail: "",
      serviceId: NO_VALUE,
      scheduledAt: "",
      notes: "",
    },
  })

  const watchedServiceId = useWatch({ control: form.control, name: "serviceId" })
  const numericServiceId =
    watchedServiceId && watchedServiceId !== NO_VALUE ? Number(watchedServiceId) : null

  function onSubmit(values: FormValues) {
    bookAppointment.mutate(
      {
        clientName: values.clientName,
        clientPhone: values.clientPhone?.length ? values.clientPhone : null,
        clientEmail: values.clientEmail?.length ? values.clientEmail : null,
        serviceId: values.serviceId && values.serviceId !== NO_VALUE ? Number(values.serviceId) : null,
        scheduledAt: values.scheduledAt,
        notes: values.notes?.length ? values.notes : null,
      },
      {
        onSuccess: (data) => setBooking(data),
        onError: () => toast.error(t("toast.createError")),
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

  if (isError || !link || !link.isActive) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background px-4 text-center">
        <h1 className="text-2xl font-semibold tracking-tight">{t("public.portal.inactive")}</h1>
      </div>
    )
  }

  if (booking) {
    const scheduledDate = new Date(booking.scheduledAt)
    return (
      <div className="min-h-screen bg-background px-4 py-12">
        <div className="mx-auto max-w-md space-y-6 text-center">
          <h1 className="text-2xl font-semibold tracking-tight">{t("public.portal.success.title")}</h1>
          <p className="text-muted-foreground">
            {t("public.portal.success.description", {
              date: dateTimeFormatter.format(scheduledDate).split(",")[0],
              time: dateTimeFormatter.format(scheduledDate).split(",")[1]?.trim() ?? "",
            })}
          </p>
          <Link
            to={`/barberia/cita/${booking.trackingToken}`}
            className="inline-flex items-center justify-center rounded-md bg-primary px-6 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            {t("public.portal.success.trackLink")}
          </Link>
        </div>
      </div>
    )
  }

  const services = link.services.filter((s) => s.isActive)

  return (
    <div className="min-h-screen bg-background px-4 py-8 sm:py-12">
      <div className="mx-auto max-w-xl space-y-6">
        <div className="text-center">
          {link.stylistName && (
            <p className="text-sm font-medium text-muted-foreground">{link.stylistName}</p>
          )}
          <h1 className="mt-1 text-2xl font-semibold tracking-tight sm:text-3xl">
            {link.title}
          </h1>
        </div>

        {services.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center text-sm text-muted-foreground">
              {t("public.portal.noServices")}
            </CardContent>
          </Card>
        ) : (
          <Card className="border-border/70">
            <CardContent className="p-6 sm:p-8">
              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
                  <FormField
                    control={form.control}
                    name="clientName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t("public.portal.form.clientName")}</FormLabel>
                        <FormControl>
                          <Input {...field} />
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
                          <FormLabel>{t("public.portal.form.clientPhone")}</FormLabel>
                          <FormControl>
                            <Input inputMode="tel" {...field} />
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
                          <FormLabel>{t("public.portal.form.clientEmail")}</FormLabel>
                          <FormControl>
                            <Input type="email" {...field} />
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
                        <FormLabel>{t("public.portal.form.service")}</FormLabel>
                        <Select value={field.value} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger className="w-full">
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value={NO_VALUE}>—</SelectItem>
                            {services.map((s) => (
                              <SelectItem key={s.id} value={String(s.id)}>
                                {s.name} — {priceFormatter.format(s.price)}
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
                    name="scheduledAt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t("public.portal.form.scheduledAt")}</FormLabel>
                        <FormControl>
                          <TimeSlotPicker
                            mode="public"
                            slug={slug!}
                            value={field.value}
                            onChange={field.onChange}
                            serviceId={numericServiceId}
                            disabled={bookAppointment.isPending}
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
                        <FormLabel>{t("public.portal.form.notes")}</FormLabel>
                        <FormControl>
                          <Textarea {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <Button
                    type="submit"
                    className="w-full h-12 text-base"
                    disabled={bookAppointment.isPending}
                  >
                    {bookAppointment.isPending
                      ? t("public.portal.form.submitting")
                      : t("public.portal.form.submit")}
                  </Button>
                </form>
              </Form>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
