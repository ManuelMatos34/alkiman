import { useCallback, useEffect, useMemo, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { ArrowLeft, Check, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"
import { cn } from "@/lib/utils"
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
import { useVehicleMakes } from "@/application/vehicles/useVehicleMakes"
import { useVehicleModels } from "@/application/vehicles/useVehicleModels"
import { VEHICLE_YEARS, VEHICLE_COLORS } from "@/domain/constants/vehicleOptions"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  CarwashKioskPaymentStep,
  type ConfirmedCarwashPayment,
} from "@/presentation/components/CarwashKioskPaymentStep"

/** Atajos de propina, en porcentaje del total. El 0 es explícito: no dejar propina tiene que ser un botón, no una omisión. */
const TIP_PERCENT_PRESETS = [0, 10, 15, 20]

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
    vehicleBrand: z.string().optional(),
    vehicleModel: z.string().optional(),
    vehicleYear: z.string().optional(),
    vehicleColor: z.string().optional(),
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

type StepId = "customer" | "vehicle" | "service" | "extras" | "tip" | "payment" | "confirm"

/**
 * Qué campos del formulario valida cada paso antes de dejar avanzar. Se valida
 * por paso y no todo al final para que el cliente corrija el error donde lo
 * cometió, en vez de que la pantalla de resumen le tire tres errores de
 * pantallas que ya dejó atrás.
 */
const FIELDS_BY_STEP: Partial<Record<StepId, (keyof JoinFormValues)[]>> = {
  customer: ["customerName", "customerPhone", "customerEmail"],
  vehicle: ["vehiclePlate", "vehicleBrand", "vehicleModel", "vehicleYear", "vehicleColor"],
  service: ["serviceId"],
}

/**
 * Pasarela pública de Carwash, con la forma de un kiosco de turnos: una pregunta
 * a la vez, en pantalla grande, y un botón para seguir.
 *
 * El recorrido no es fijo. Los pasos de agregados, propina y pago sólo existen
 * si el negocio los tiene: mostrar un paso vacío "no hay agregados" o pedir una
 * propina a un negocio que no las maneja son pasos que el cliente tiene que
 * despachar sin recibir nada a cambio.
 */
export function CarwashPortalJoinPage() {
  const { t, i18n } = useTranslation("carwash")
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const { data: link, isLoading, isError } = useCarwashPublicLink(slug)
  const joinQueue = useJoinCarwashQueue(slug)
  const { data: makes } = useVehicleMakes()

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

  const [stepIndex, setStepIndex] = useState(0)
  const [tipAmount, setTipAmount] = useState(0)
  const [tipTouched, setTipTouched] = useState(false)
  const [confirmedPayment, setConfirmedPayment] = useState<ConfirmedCarwashPayment | null>(null)
  const [chargedAmount, setChargedAmount] = useState<number | null>(null)
  // Se enciende si el paso de pago descubre en vivo que no hay pasarela usable
  // (credenciales retiradas, config rota). El turno igual se toma y se paga en
  // el mostrador: no dejar entrar al cliente sería el peor de los dos errores.
  const [paymentUnavailable, setPaymentUnavailable] = useState(false)

  const selectedMakeId = form.watch("vehicleBrand") ? Number(form.watch("vehicleBrand")) : null
  const { data: models } = useVehicleModels(selectedMakeId)

  const selectedServiceId = form.watch("serviceId")
  const selectedExtraIds = form.watch("extraIds")

  const selectedService = link?.services.find((s) => String(s.id) === selectedServiceId)
  const selectedExtras = (link?.extras ?? []).filter((extra) => selectedExtraIds.includes(extra.id))

  /**
   * Total del servicio: NO incluye propina. Es el número que se factura.
   * Sin `useMemo` a propósito: `selectedExtras` se rearma en cada render, así que
   * memorizar sobre él no ahorra nada y sólo disfrazaría la dependencia.
   */
  const serviceTotal =
    (selectedService?.price ?? 0) + selectedExtras.reduce((sum, e) => sum + e.price, 0)

  const tipEnabled = !!link && link.tipMode !== "Disabled"
  const paymentEnabled = !!link && link.paymentEnabled && !paymentUnavailable

  const steps = useMemo<StepId[]>(() => {
    const list: StepId[] = ["customer", "vehicle", "service"]
    if ((link?.extras.length ?? 0) > 0) list.push("extras")
    if (tipEnabled) list.push("tip")
    if (paymentEnabled) list.push("payment")
    list.push("confirm")
    return list
  }, [link, tipEnabled, paymentEnabled])

  // Si el paso de pago desaparece a mitad de camino, el índice puede quedar
  // apuntando fuera de la lista. Se re-encaja en el último paso en vez de
  // renderizar una pantalla en blanco.
  useEffect(() => {
    setStepIndex((current) => Math.min(current, steps.length - 1))
  }, [steps.length])

  // Con propina sugerida el monto llega pre-cargado, pero deja de moverse en
  // cuanto el cliente lo toca: recalcularlo por debajo le cambiaría lo que ya
  // eligió.
  useEffect(() => {
    if (!link || link.tipMode !== "Suggested" || tipTouched) return
    setTipAmount(Math.round(serviceTotal * link.tipSuggestedPercent) / 100)
  }, [link, serviceTotal, tipTouched])

  const currentStep = steps[stepIndex] ?? "confirm"
  const effectiveTip = tipEnabled ? tipAmount : 0
  const grandTotal = serviceTotal + (paymentEnabled ? effectiveTip : 0)

  const paymentQuote = useMemo(
    () => ({
      serviceId: Number(selectedServiceId || 0),
      extraIds: selectedExtraIds,
      tipAmount: effectiveTip,
    }),
    [selectedServiceId, selectedExtraIds, effectiveTip]
  )

  const handlePaymentUnavailable = useCallback(() => setPaymentUnavailable(true), [])
  const handleAmountResolved = useCallback((amount: number) => setChargedAmount(amount), [])
  const handlePaymentConfirmed = useCallback((payment: ConfirmedCarwashPayment) => {
    setConfirmedPayment(payment)
    setStepIndex((current) => current + 1)
  }, [])

  async function goNext() {
    const fields = FIELDS_BY_STEP[currentStep]
    if (fields && !(await form.trigger(fields))) return
    setStepIndex((current) => Math.min(current + 1, steps.length - 1))
  }

  function goBack() {
    setStepIndex((current) => Math.max(current - 1, 0))
  }

  function submit() {
    const values = form.getValues()
    joinQueue.mutate(
      {
        customerName: values.customerName,
        customerPhone: values.customerPhone,
        customerEmail: values.customerEmail?.length ? values.customerEmail : null,
        serviceId: Number(values.serviceId),
        extraIds: values.extraIds,
        vehiclePlate: values.vehiclePlate,
        vehicleBrand: makes?.find((m) => String(m.id) === values.vehicleBrand)?.name ?? null,
        vehicleModel: models?.find((m) => String(m.id) === values.vehicleModel)?.name ?? null,
        vehicleYear: values.vehicleYear?.length ? Number(values.vehicleYear) : null,
        vehicleColor: values.vehicleColor?.length ? values.vehicleColor : null,
        // La propina sólo viaja si de verdad se cobró. Mandarla en un turno que
        // se paga en el mostrador anotaría plata que nadie entregó todavía.
        tipAmount: confirmedPayment ? effectiveTip : null,
        paymentProvider: confirmedPayment?.provider ?? null,
        paymentReference: confirmedPayment?.reference ?? null,
      },
      {
        onSuccess: (ticket) => navigate(`/lavado/turno/${ticket.accessToken}`),
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
    <div className="min-h-screen bg-background px-4 py-8 sm:py-12">
      <PublicAppearanceSync themeMode={link.themeMode} accentColor={link.accentColor} />
      <div className="mx-auto max-w-xl space-y-6">
        <div className="text-center">
          <p className="text-sm font-medium text-muted-foreground">{link.businessName}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight sm:text-3xl">{link.linkTitle}</h1>
        </div>

        <StepProgress steps={steps} currentIndex={stepIndex} />

        <Card className="border-border/70">
          <CardContent className="space-y-6 p-6 sm:p-8">
            <div className="space-y-1 text-center">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {t("kiosk.stepCounter", { current: stepIndex + 1, total: steps.length })}
              </p>
              <h2 className="text-xl font-semibold tracking-tight sm:text-2xl">
                {t(`kiosk.steps.${currentStep}.title`)}
              </h2>
              <p className="text-sm text-muted-foreground">
                {t(`kiosk.steps.${currentStep}.subtitle`)}
              </p>
            </div>

            <Form {...form}>
              {/* Sin <form>: el submit real lo dispara el último paso. Un Enter que
                  enviara el turno desde el primer campo sería un turno a medio armar. */}
              {currentStep === "customer" && (
                <div className="space-y-5">
                  <FormField
                    control={form.control}
                    name="customerName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="text-base">{t("join.fields.customerName")}</FormLabel>
                        <FormControl>
                          <Input
                            autoFocus
                            className="h-14 text-base"
                            placeholder={t("join.fields.customerNamePlaceholder")}
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="customerPhone"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="text-base">{t("join.fields.customerPhone")}</FormLabel>
                        <FormControl>
                          <Input
                            inputMode="tel"
                            className="h-14 text-base"
                            placeholder={t("join.fields.customerPhonePlaceholder")}
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
                        <FormLabel className="text-base">
                          {t("join.fields.customerEmail")}{" "}
                          <span className="text-muted-foreground">
                            ({t("common:labels.optional")})
                          </span>
                        </FormLabel>
                        <FormControl>
                          <Input
                            type="email"
                            className="h-14 text-base"
                            placeholder={t("join.fields.customerEmailPlaceholder")}
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              )}

              {currentStep === "vehicle" && (
                <div className="space-y-5">
                  <FormField
                    control={form.control}
                    name="vehiclePlate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="text-base">{t("join.fields.vehiclePlate")}</FormLabel>
                        <FormControl>
                          <Input
                            autoFocus
                            className="h-14 text-center text-lg font-semibold uppercase tracking-widest"
                            placeholder={t("join.fields.vehiclePlatePlaceholder")}
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
                          <Select
                            onValueChange={(value) => {
                              field.onChange(value)
                              form.setValue("vehicleModel", "")
                            }}
                            value={field.value}
                          >
                            <FormControl>
                              <SelectTrigger className="h-12 w-full">
                                <SelectValue placeholder={t("vehicle.placeholders.brand")} />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent >
                              {(makes ?? []).map((make) => (
                                <SelectItem key={make.id} value={String(make.id)}>
                                  {make.name}
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
                      name="vehicleModel"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("vehicle.fields.model")}</FormLabel>
                          <Select
                            onValueChange={field.onChange}
                            value={field.value}
                            disabled={!selectedMakeId}
                          >
                            <FormControl>
                              <SelectTrigger className="h-12 w-full">
                                <SelectValue placeholder={t("vehicle.placeholders.model")} />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent >
                              {(models ?? []).map((model) => (
                                <SelectItem key={model.id} value={String(model.id)}>
                                  {model.name}
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
                      name="vehicleYear"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("vehicle.fields.year")}</FormLabel>
                          <Select onValueChange={field.onChange} value={field.value}>
                            <FormControl>
                              <SelectTrigger className="h-12 w-full">
                                <SelectValue placeholder={t("vehicle.placeholders.year")} />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent >
                              {VEHICLE_YEARS.map((year) => (
                                <SelectItem key={year} value={year}>
                                  {year}
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
                      name="vehicleColor"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("vehicle.fields.color")}</FormLabel>
                          <Select onValueChange={field.onChange} value={field.value}>
                            <FormControl>
                              <SelectTrigger className="h-12 w-full">
                                <SelectValue placeholder={t("vehicle.placeholders.color")} />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent >
                              {VEHICLE_COLORS.map((color) => (
                                <SelectItem key={color} value={color}>
                                  {color}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                </div>
              )}

              {currentStep === "service" && (
                <FormField
                  control={form.control}
                  name="serviceId"
                  render={({ field }) => (
                    <FormItem>
                      <div className="space-y-3">
                        {!hasServices && (
                          <p className="py-6 text-center text-sm text-muted-foreground">
                            {t("join.fields.serviceEmpty")}
                          </p>
                        )}
                        {link.services.map((service) => (
                          <ChoiceTile
                            key={service.id}
                            selected={field.value === String(service.id)}
                            title={service.name}
                            description={service.description}
                            meta={t("extras.table.minutesValue", { count: service.estimatedMinutes })}
                            price={currencyFormatter.format(service.price)}
                            onClick={() => field.onChange(String(service.id))}
                          />
                        ))}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              {currentStep === "extras" && (
                <FormField
                  control={form.control}
                  name="extraIds"
                  render={({ field }) => (
                    <FormItem>
                      <div className="space-y-3">
                        {link.extras.map((extra) => (
                          <ChoiceTile
                            key={extra.id}
                            selected={field.value.includes(extra.id)}
                            title={extra.name}
                            description={extra.description}
                            meta={t("extras.table.minutesValue", { count: extra.estimatedMinutes })}
                            price={`+ ${currencyFormatter.format(extra.price)}`}
                            onClick={() =>
                              field.onChange(
                                field.value.includes(extra.id)
                                  ? field.value.filter((id) => id !== extra.id)
                                  : [...field.value, extra.id]
                              )
                            }
                          />
                        ))}
                      </div>
                    </FormItem>
                  )}
                />
              )}
            </Form>

            {currentStep === "tip" && (
              <div className="space-y-5">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                  {TIP_PERCENT_PRESETS.map((percent) => {
                    const value = Math.round(serviceTotal * percent) / 100
                    const isSelected = Math.abs(value - tipAmount) < 0.005
                    return (
                      <button
                        key={percent}
                        type="button"
                        onClick={() => {
                          setTipTouched(true)
                          setTipAmount(value)
                        }}
                        className={cn(
                          "flex h-20 flex-col items-center justify-center rounded-xl border-2 transition-colors",
                          isSelected
                            ? "border-primary bg-primary/10"
                            : "border-border/70 hover:border-primary/50"
                        )}
                      >
                        <span className="text-lg font-semibold">
                          {percent === 0 ? t("kiosk.tip.none") : `${percent}%`}
                        </span>
                        {percent > 0 && (
                          <span className="text-xs text-muted-foreground">
                            {currencyFormatter.format(value)}
                          </span>
                        )}
                      </button>
                    )
                  })}
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="kiosk-tip-custom">
                    {t("kiosk.tip.customLabel")}
                  </label>
                  <Input
                    id="kiosk-tip-custom"
                    inputMode="decimal"
                    className="h-14 text-base"
                    value={tipAmount === 0 ? "" : String(tipAmount)}
                    placeholder="0.00"
                    onChange={(event) => {
                      setTipTouched(true)
                      const parsed = Number(event.target.value)
                      setTipAmount(Number.isFinite(parsed) && parsed > 0 ? parsed : 0)
                    }}
                  />
                  <p className="text-xs text-muted-foreground">
                    {paymentEnabled ? t("kiosk.tip.hintCharged") : t("kiosk.tip.hintCounter")}
                  </p>
                </div>
              </div>
            )}

            {currentStep === "payment" && (
              <div className="space-y-5">
                <div className="flex items-center justify-between rounded-xl bg-muted/60 px-4 py-4">
                  <span className="text-sm font-medium">{t("kiosk.summary.toPay")}</span>
                  <span className="text-2xl font-semibold">
                    {currencyFormatter.format(chargedAmount ?? grandTotal)}
                  </span>
                </div>
                <CarwashKioskPaymentStep
                  slug={slug}
                  quote={paymentQuote}
                  confirmedPayment={confirmedPayment}
                  onConfirmed={handlePaymentConfirmed}
                  onAmountResolved={handleAmountResolved}
                  onUnavailable={handlePaymentUnavailable}
                />
              </div>
            )}

            {currentStep === "confirm" && (
              <div className="space-y-3">
                <SummaryRow label={t("join.fields.customerName")} value={form.getValues("customerName")} />
                <SummaryRow label={t("join.fields.vehiclePlate")} value={form.getValues("vehiclePlate")} />
                <SummaryRow
                  label={t("join.fields.service")}
                  value={selectedService?.name ?? "—"}
                  amount={selectedService ? currencyFormatter.format(selectedService.price) : undefined}
                />
                {selectedExtras.map((extra) => (
                  <SummaryRow
                    key={extra.id}
                    label={t("extras.pickerLabel")}
                    value={extra.name}
                    amount={currencyFormatter.format(extra.price)}
                  />
                ))}
                {confirmedPayment && effectiveTip > 0 && (
                  <SummaryRow
                    label={t("kiosk.summary.tip")}
                    value={t("kiosk.summary.tipPaid")}
                    amount={currencyFormatter.format(effectiveTip)}
                  />
                )}
                <div className="flex items-center justify-between rounded-xl bg-muted/60 px-4 py-4">
                  <span className="text-sm font-medium">
                    {confirmedPayment ? t("kiosk.summary.paid") : t("kiosk.summary.payAtCounter")}
                  </span>
                  <span className="text-2xl font-semibold">
                    {currencyFormatter.format(
                      confirmedPayment ? (chargedAmount ?? grandTotal) : serviceTotal
                    )}
                  </span>
                </div>
              </div>
            )}

            <div className="flex gap-3 pt-2">
              {stepIndex > 0 && (
                <Button
                  type="button"
                  variant="outline"
                  className="h-14 flex-1 text-base"
                  onClick={goBack}
                  disabled={isPending}
                >
                  <ArrowLeft className="mr-1 h-4 w-4" />
                  {t("kiosk.back")}
                </Button>
              )}

              {currentStep === "confirm" ? (
                <Button
                  type="button"
                  className="h-14 flex-[2] text-base"
                  onClick={submit}
                  disabled={isPending || !hasServices}
                >
                  {isPending ? t("join.submitting") : t("join.submit")}
                </Button>
              ) : currentStep === "payment" ? null : (
                <Button
                  type="button"
                  className="h-14 flex-[2] text-base"
                  onClick={goNext}
                  disabled={currentStep === "service" && !hasServices}
                >
                  {t("kiosk.next")}
                </Button>
              )}
            </div>
          </CardContent>
        </Card>

        {currentStep !== "confirm" && serviceTotal > 0 && (
          <div className="flex items-center justify-between px-2 text-sm text-muted-foreground">
            <span>{t("extras.total")}</span>
            <span className="font-semibold text-foreground">
              {currencyFormatter.format(grandTotal)}
            </span>
          </div>
        )}
      </div>
    </div>
  )
}

/** Barra de puntos: cuántos pasos faltan, sin nombrarlos todos (en un kiosco no hay lugar). */
function StepProgress({ steps, currentIndex }: { steps: StepId[]; currentIndex: number }) {
  return (
    <div className="flex items-center justify-center gap-2">
      {steps.map((step, index) => (
        <div
          key={step}
          className={cn(
            "h-2 rounded-full transition-all",
            index < currentIndex && "w-8 bg-primary/50",
            index === currentIndex && "w-10 bg-primary",
            index > currentIndex && "w-2 bg-border"
          )}
        />
      ))}
    </div>
  )
}

interface ChoiceTileProps {
  selected: boolean
  title: string
  description?: string | null
  meta?: string
  price: string
  onClick: () => void
}

/** Tarjeta grande de selección: en un kiosco se toca con el dedo, no se apunta con el mouse. */
function ChoiceTile({ selected, title, description, meta, price, onClick }: ChoiceTileProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex w-full items-center gap-4 rounded-xl border-2 p-4 text-left transition-colors",
        selected ? "border-primary bg-primary/5" : "border-border/70 hover:border-primary/50"
      )}
    >
      <div
        className={cn(
          "flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2",
          selected ? "border-primary bg-primary text-primary-foreground" : "border-border"
        )}
      >
        {selected && <Check className="h-4 w-4" />}
      </div>
      <div className="min-w-0 flex-1">
        <p className="font-medium">{title}</p>
        {description && <p className="truncate text-sm text-muted-foreground">{description}</p>}
        {meta && <p className="text-xs text-muted-foreground">{meta}</p>}
      </div>
      <span className="shrink-0 font-semibold">{price}</span>
    </button>
  )
}

function SummaryRow({ label, value, amount }: { label: string; value: string; amount?: string }) {
  return (
    <div className="flex items-start justify-between gap-4 border-b border-border/50 pb-3 text-sm">
      <div className="min-w-0">
        <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
        <p className="font-medium">{value || "—"}</p>
      </div>
      {amount && <span className="shrink-0 font-medium">{amount}</span>}
    </div>
  )
}
