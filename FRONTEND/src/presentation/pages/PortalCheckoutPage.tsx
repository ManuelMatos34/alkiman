import { useMemo, useRef, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Link, useParams } from "react-router-dom"
import { isAxiosError } from "axios"
import { Trans, useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { CheckCircle2, ChevronLeft, Loader2 } from "lucide-react"
import { SignaturePad, type SignaturePadHandle } from "@/presentation/components/SignaturePad"
import { PortalPaymentStep } from "@/presentation/components/PortalPaymentStep"
import { PortalContractStep } from "@/presentation/components/PortalContractStep"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { cn } from "@/lib/utils"
import { usePortalCatalog } from "@/application/portal/usePortalCatalog"
import { usePortalCheckout } from "@/application/portal/usePortalCheckout"
import {
  addRentalPeriods,
  rentalPeriodUnitLabels,
  rentalPeriodUnitPluralLabels,
  rentalTypeLabels,
} from "@/domain/types/asset"
import type { PortalCheckoutResponse } from "@/domain/types/portalPublic"
import type { ConfirmedPortalPayment } from "@/domain/types/portalPayment"
import type { PortalContractPreviewRequest } from "@/domain/types/portalContractPreview"
import { PublicAppearanceSync } from "@/presentation/components/PublicAppearanceSync"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"

function buildCheckoutFormSchema(t: TFunction) {
  return z
    .object({
      fullName: z
        .string()
        .trim()
        .min(1, t("checkout.validation.fullNameRequired"))
        .max(150, t("checkout.validation.fullNameMax")),
      email: z
        .string()
        .trim()
        .min(1, t("checkout.validation.emailRequired"))
        .email(t("checkout.validation.emailInvalid")),
      phone: z.string().trim().max(50, t("checkout.validation.phoneMax")).optional(),
      address: z.string().trim().max(250, t("checkout.validation.addressMax")).optional(),
      country: z.string().trim().max(100, t("checkout.validation.countryMax")).optional(),

      startDate: z.string().min(1, t("checkout.validation.startDateRequired")),
      periods: z
        .string()
        .min(1, t("checkout.validation.quantityRequired"))
        .refine((val) => !Number.isNaN(Number(val)), t("checkout.validation.quantityInvalid"))
        .refine((val) => Number.isInteger(Number(val)), t("checkout.validation.quantityInteger"))
        .refine((val) => Number(val) >= 1, t("checkout.validation.quantityMin")),
      quantity: z
        .string()
        .min(1, t("checkout.validation.quantityRequired"))
        .refine((val) => !Number.isNaN(Number(val)), t("checkout.validation.quantityInvalid"))
        .refine((val) => Number.isInteger(Number(val)), t("checkout.validation.quantityInteger"))
        .refine((val) => Number(val) >= 1, t("checkout.validation.quantityMin")),

      signatureBase64: z.string().min(1, t("checkout.validation.signatureRequired")),
    })
}

type CheckoutFormValues = z.infer<ReturnType<typeof buildCheckoutFormSchema>>

const stepFields: (keyof CheckoutFormValues)[][] = [
  ["fullName", "email", "phone", "address", "country"],
  ["startDate", "periods", "quantity"],
  [], // paso 2: previsualización del contrato (estado local `contractAccepted`, no es RHF)
  ["signatureBase64"], // paso 3: firma
  [], // paso 4: pago (estado local `confirmedPayment`, no es RHF)
]

/**
 * Pasarela pública de 5 pasos del Portal de Rentas: datos personales, detalle de la renta,
 * previsualización y aceptación del contrato, firma digital y pago real en modo sandbox
 * (Stripe). El pago va al final para que el cliente lea y firme el contrato antes
 * de que se le cobre.
 */
export function PortalCheckoutPage() {
  const { t, i18n } = useTranslation("portal")
  const { slug, assetId } = useParams<{ slug: string; assetId: string }>()
  const { data: catalog, isLoading, isError } = usePortalCatalog(slug)
  const checkout = usePortalCheckout(slug)

  const [step, setStep] = useState(0)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [result, setResult] = useState<PortalCheckoutResponse | null>(null)
  const [confirmedPayment, setConfirmedPayment] = useState<ConfirmedPortalPayment | null>(null)
  const [contractAccepted, setContractAccepted] = useState(false)
  const signaturePadRef = useRef<SignaturePadHandle>(null)

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(getIntlLocale(i18n.language), {
        style: "currency",
        currency: "DOP",
      }),
    [i18n.language]
  )

  const stepLabels = useMemo(
    () => [
      t("checkout.steps.personalData"),
      t("checkout.steps.rentalDetail"),
      t("checkout.steps.contract"),
      t("checkout.steps.signature"),
      t("checkout.steps.payment"),
    ],
    [t]
  )

  const checkoutFormSchema = useMemo(() => buildCheckoutFormSchema(t), [t])

  const form = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutFormSchema),
    defaultValues: {
      fullName: "",
      email: "",
      phone: "",
      address: "",
      country: "",
      startDate: "",
      periods: "1",
      quantity: "1",
      signatureBase64: "",
    },
  })

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  const asset = catalog?.assets.find((a) => a.id === assetId)

  if (isError || !catalog || !asset) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2 bg-background text-center px-4">
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("checkout.assetUnavailable.title")}
        </h1>
        <p className="text-sm text-muted-foreground">
          {t("checkout.assetUnavailable.description")}
        </p>
        <Button asChild variant="outline" size="sm" className="mt-2">
          <Link to={`/p/${slug}`}>{t("checkout.backToCatalog")}</Link>
        </Button>
      </div>
    )
  }

  const quantity = Number(form.watch("quantity")) || 0
  const periods = Number(form.watch("periods")) || 0
  const totalPrice = asset.basePrice * periods * quantity

  const startDateValue = form.watch("startDate")
  const estimatedEndDate =
    startDateValue && periods >= 1
      ? addRentalPeriods(new Date(`${startDateValue}T00:00:00`), asset.rentalType, periods)
      : null
  const estimatedEndDateLabel = estimatedEndDate
    ? new Intl.DateTimeFormat(getIntlLocale(i18n.language), { dateStyle: "medium" }).format(
        estimatedEndDate
      )
    : null

  const paymentQuote = { assetId: asset.id, periods: periods || 1, quantity: quantity || 1 }

  const contractPreviewRequest: PortalContractPreviewRequest = {
    assetId: asset.id,
    fullName: form.watch("fullName"),
    email: form.watch("email") || null,
    phone: form.watch("phone") || null,
    address: form.watch("address") || null,
    identityNumber: null,
    startDate: startDateValue,
    periods: periods || 1,
    quantity: quantity || 1,
  }

  async function handleNext() {
    if (step === 2 && !contractAccepted) return
    if (step === 4 && !confirmedPayment) return
    // Misma red de seguridad que en el submit final: si el usuario dibujó la firma pero
    // ningún callback de auto-guardado de SignaturePad disparó onChange todavía, la
    // sincronizamos acá mismo justo antes de validar, en vez de depender únicamente de
    // los callbacks internos de la librería del lienzo. Necesario porque la firma ya no es
    // el último paso (usa "Continuar"/handleNext, no el onSubmit del <form>).
    if (step === 3 && !form.getValues("signatureBase64")) {
      const base64 = signaturePadRef.current?.trySave()
      if (base64) {
        form.setValue("signatureBase64", base64, { shouldValidate: false })
      }
    }
    const fields = stepFields[step]
    const valid = fields.length ? await form.trigger(fields) : true
    if (!valid) return
    setStep((s) => Math.min(s + 1, stepFields.length - 1))
  }

  function handleBack() {
    setSubmitError(null)
    // Volver del paso de contrato hacia atrás invalida la aceptación: pudo cambiar el
    // detalle de la renta (periods/quantity), así que hay que releer y aceptar de nuevo.
    if (step === 2) {
      setContractAccepted(false)
    }
    // Volver del paso de pago al de firma invalida el pago sandbox ya confirmado: el monto
    // pudo haber cambiado, así que se limpia y hay que repetirlo.
    if (step === 4) {
      setConfirmedPayment(null)
    }
    setStep((s) => Math.max(s - 1, 0))
  }

  async function onSubmit(values: CheckoutFormValues) {
    if (!asset || !confirmedPayment) return
    setSubmitError(null)
    try {
      const response = await checkout.mutateAsync({
        assetId: asset.id,
        fullName: values.fullName,
        email: values.email,
        phone: values.phone || null,
        address: values.address || null,
        country: values.country || null,
        startDate: values.startDate,
        periods: Number(values.periods),
        quantity: Number(values.quantity),
        paymentProvider: confirmedPayment.provider,
        paymentReference: confirmedPayment.reference,
        signatureImageBase64: values.signatureBase64,
      })
      setResult(response)
    } catch (error) {
      if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
        setSubmitError(error.response.data.detail)
      } else {
        setSubmitError(t("checkout.errors.checkoutFailed"))
      }
    }
  }

  if (result) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <PublicAppearanceSync themeMode={catalog.themeMode} accentColor={catalog.accentColor} />
        <Card className="w-full max-w-md text-center">
          <CardHeader className="items-center">
            <CheckCircle2 className="h-12 w-12 text-primary" />
            <CardTitle className="text-xl">{t("checkout.success.title")}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-muted-foreground">
            <p>
              <Trans
                t={t}
                i18nKey="checkout.success.summary"
                values={{
                  assetName: asset.name,
                  price: currencyFormatter.format(result.totalPrice),
                }}
                components={{
                  1: <span className="font-medium text-foreground" />,
                  2: <span className="font-medium text-foreground" />,
                }}
              />
            </p>
            <p>
              {t("checkout.success.paymentVerified", {
                provider: result.paymentProvider,
              })}
            </p>
            <p>{t("checkout.success.emailNotice")}</p>
            <p>{t("checkout.success.canClose")}</p>
            <Button asChild className="mt-2 w-full">
              <Link to={`/p/${slug}`}>{t("checkout.success.backToCatalog")}</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <PublicAppearanceSync themeMode={catalog.themeMode} accentColor={catalog.accentColor} />
      <header className="border-b border-border/60 bg-card">
        <div className="mx-auto max-w-2xl px-4 py-6">
          <Link
            to={`/p/${slug}`}
            className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          >
            <ChevronLeft className="h-4 w-4" />
            {t("checkout.backToCatalog")}
          </Link>
          <h1 className="mt-2 text-xl font-semibold tracking-tight">
            {t("checkout.title", { assetName: asset.name })}
          </h1>
        </div>
      </header>

      <main className="mx-auto max-w-2xl px-4 py-8">
        <div className="mb-6 flex items-center gap-2">
          {stepLabels.map((label, index) => (
            <div key={label} className="flex flex-1 items-center gap-2">
              <div
                className={cn(
                  "flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-medium",
                  index === step
                    ? "bg-primary text-primary-foreground"
                    : index < step
                      ? "bg-primary/20 text-primary"
                      : "bg-muted text-muted-foreground"
                )}
              >
                {index + 1}
              </div>
              <span
                className={cn(
                  "hidden text-sm sm:inline",
                  index === step ? "font-medium text-foreground" : "text-muted-foreground"
                )}
              >
                {label}
              </span>
              {index < stepLabels.length - 1 && (
                <div className="h-px flex-1 bg-border" />
              )}
            </div>
          ))}
        </div>

        <Card>
          <CardContent className="pt-6">
            <Form {...form}>
              <form
                onSubmit={(e) => {
                  // Red de seguridad: si el usuario dibujó la firma pero, por lo que sea, el
                  // autoguardado de SignaturePad nunca disparó onChange, la sincronizamos acá
                  // mismo justo antes de que react-hook-form valide, en vez de depender
                  // únicamente de los callbacks internos de la librería del lienzo.
                  if (!form.getValues("signatureBase64")) {
                    const base64 = signaturePadRef.current?.trySave()
                    if (base64) {
                      form.setValue("signatureBase64", base64, { shouldValidate: false })
                    }
                  }
                  return form.handleSubmit(onSubmit)(e)
                }}
                className="space-y-4"
                onKeyDown={(e) => {
                  if (e.key === "Enter" && step < stepFields.length - 1) {
                    e.preventDefault()
                  }
                }}
              >
                {step === 0 && (
                  <div className="space-y-4">
                    <FormField
                      control={form.control}
                      name="fullName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("checkout.form.fullName.label")}</FormLabel>
                          <FormControl>
                            <Input placeholder={t("checkout.form.fullName.placeholder")} {...field} />
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
                          <FormLabel>{t("checkout.form.email.label")}</FormLabel>
                          <FormControl>
                            <Input
                              type="email"
                              placeholder={t("checkout.form.email.placeholder")}
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
                          <FormLabel>{t("checkout.form.phone.label")}</FormLabel>
                          <FormControl>
                            <Input placeholder={t("common:labels.optional")} {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="address"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("checkout.form.address.label")}</FormLabel>
                          <FormControl>
                            <Input placeholder={t("common:labels.optional")} {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="country"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("checkout.form.country.label")}</FormLabel>
                          <FormControl>
                            <Input placeholder={t("common:labels.optional")} {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                )}

                {step === 1 && (
                  <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">
                      <Trans
                        t={t}
                        i18nKey="checkout.rentalDetail.info"
                        values={{
                          rentalType: rentalTypeLabels[asset.rentalType].toLowerCase(),
                          unit: rentalPeriodUnitLabels[asset.rentalType],
                        }}
                        components={{ 1: <span className="font-medium text-foreground" /> }}
                      />
                    </p>
                    <div className="grid grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="startDate"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>{t("checkout.rentalDetail.startDate")}</FormLabel>
                            <FormControl>
                              <Input type="date" {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                      <FormField
                        control={form.control}
                        name="periods"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>
                              {t("checkout.rentalDetail.periodsLabel", {
                                unit: rentalPeriodUnitPluralLabels[asset.rentalType],
                              })}
                            </FormLabel>
                            <FormControl>
                              <Input type="number" min={1} step={1} {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>
                    <FormField
                      control={form.control}
                      name="quantity"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("checkout.rentalDetail.quantityLabel")}</FormLabel>
                          <FormControl>
                            <Input type="number" min={1} step={1} {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <div className="rounded-lg border border-border/60 bg-muted/40 p-3 text-sm">
                      {estimatedEndDateLabel && (
                        <div className="flex justify-between text-muted-foreground">
                          <span>{t("checkout.rentalDetail.estimatedEndDate")}</span>
                          <span>{estimatedEndDateLabel}</span>
                        </div>
                      )}
                      <div className="flex justify-between text-muted-foreground">
                        <span>
                          {t("checkout.rentalDetail.basePrice", {
                            rentalType: rentalTypeLabels[asset.rentalType].toLowerCase(),
                          })}
                        </span>
                        <span>{currencyFormatter.format(asset.basePrice)}</span>
                      </div>
                      <div className="mt-1 flex justify-between font-medium">
                        <span>{t("checkout.rentalDetail.total")}</span>
                        <span>{currencyFormatter.format(totalPrice)}</span>
                      </div>
                    </div>
                  </div>
                )}

                {step === 2 && (
                  <PortalContractStep
                    slug={slug}
                    request={contractPreviewRequest}
                    accepted={contractAccepted}
                    onAcceptedChange={setContractAccepted}
                  />
                )}

                {step === 3 && (
                  <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">{t("checkout.signature.info")}</p>
                    <FormField
                      control={form.control}
                      name="signatureBase64"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t("checkout.signature.label")}</FormLabel>
                          <FormControl>
                            <SignaturePad
                              ref={signaturePadRef}
                              onSave={(base64Png) =>
                                field.onChange(base64Png)
                              }
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                )}

                {step === 4 && (
                  <PortalPaymentStep
                    slug={slug}
                    quote={paymentQuote}
                    confirmedPayment={confirmedPayment}
                    onConfirmed={(payment) => {
                      setConfirmedPayment(payment)
                      setStep((s) => Math.min(s + 1, stepFields.length - 1))
                    }}
                    onReset={() => setConfirmedPayment(null)}
                  />
                )}

                {submitError && <p className="text-sm text-destructive">{submitError}</p>}

                <div className="flex justify-between pt-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleBack}
                    disabled={step === 0 || checkout.isPending}
                  >
                    {t("common:buttons.back")}
                  </Button>
                  {step < stepFields.length - 1 ? (
                    <Button
                      type="button"
                      onClick={handleNext}
                      disabled={(step === 2 && !contractAccepted) || (step === 4 && !confirmedPayment)}
                    >
                      {t("common:buttons.continue")}
                    </Button>
                  ) : (
                    <Button type="submit" disabled={checkout.isPending}>
                      {checkout.isPending ? t("common:status.processing") : t("checkout.nav.confirm")}
                    </Button>
                  )}
                </div>
              </form>
            </Form>
          </CardContent>
        </Card>
      </main>
    </div>
  )
}
