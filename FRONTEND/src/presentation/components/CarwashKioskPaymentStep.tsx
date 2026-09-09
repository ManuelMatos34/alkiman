import { useEffect, useMemo, useState } from "react"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import { CheckCircle2, FlaskConical, Loader2 } from "lucide-react"
import { loadStripe } from "@stripe/stripe-js"
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js"
import { Button } from "@/components/ui/button"
import { useCarwashPublicPaymentConfig } from "@/application/carwash/useCarwashPublicPaymentConfig"
import { useCreateCarwashStripeIntent } from "@/application/carwash/useCreateCarwashStripeIntent"
import type { CarwashPaymentIntentRequest } from "@/domain/types/carwashPublic"

/** El pago confirmado contra Stripe, tal como viaja después al join. */
export interface ConfirmedCarwashPayment {
  provider: "Stripe"
  reference: string
}

function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

interface StripeConfirmFormProps {
  onConfirmed: (payment: ConfirmedCarwashPayment) => void
}

/** Vive DENTRO de `<Elements>`: `useStripe`/`useElements` no funcionan fuera del provider. */
function StripeConfirmForm({ onConfirmed }: StripeConfirmFormProps) {
  const { t } = useTranslation("carwash")
  const stripe = useStripe()
  const elements = useElements()
  const [isConfirming, setIsConfirming] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleConfirm() {
    if (!stripe || !elements) return
    setIsConfirming(true)
    setError(null)
    const { error: confirmError, paymentIntent } = await stripe.confirmPayment({
      elements,
      redirect: "if_required",
    })
    setIsConfirming(false)
    if (confirmError) {
      setError(confirmError.message ?? t("kiosk.payment.genericError"))
      return
    }
    if (paymentIntent?.status === "succeeded") {
      onConfirmed({ provider: "Stripe", reference: paymentIntent.id })
    } else {
      setError(t("kiosk.payment.genericError"))
    }
  }

  return (
    <div className="space-y-4">
      <PaymentElement />
      <p className="text-xs text-muted-foreground">
        {t("kiosk.payment.testCardHint", { cardNumber: "4242 4242 4242 4242" })}
      </p>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button
        type="button"
        onClick={handleConfirm}
        disabled={!stripe || !elements || isConfirming}
        className="h-14 w-full text-base"
      >
        {isConfirming ? t("kiosk.payment.confirming") : t("kiosk.payment.confirmButton")}
      </Button>
    </div>
  )
}

interface StripePanelProps {
  slug: string | undefined
  publishableKey: string
  quote: CarwashPaymentIntentRequest
  onConfirmed: (payment: ConfirmedCarwashPayment) => void
  onAmountResolved: (amount: number) => void
}

function StripePanel({ slug, publishableKey, quote, onConfirmed, onAmountResolved }: StripePanelProps) {
  const { t } = useTranslation("carwash")
  const stripePromise = useMemo(() => loadStripe(publishableKey), [publishableKey])
  const { mutate: createIntent } = useCreateCarwashStripeIntent(slug)
  const [clientSecret, setClientSecret] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  // La lista de extras se serializa para la lista de dependencias: es un array
  // nuevo en cada render y compararlo por referencia recrearía el intent en bucle.
  const extrasKey = (quote.extraIds ?? []).join(",")

  useEffect(() => {
    let cancelled = false
    setClientSecret(null)
    setLoadError(null)
    createIntent(quote, {
      onSuccess: (data) => {
        if (cancelled) return
        setClientSecret(data.clientSecret)
        // El monto que manda es el del servidor, no la suma del navegador: es el
        // que de verdad se va a cobrar.
        onAmountResolved(data.amount)
      },
      onError: (error) => {
        if (!cancelled) setLoadError(extractErrorMessage(error, t("kiosk.payment.genericError")))
      },
    })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [createIntent, quote.serviceId, extrasKey, quote.tipAmount, t])

  if (loadError) {
    return <p className="py-4 text-sm text-destructive">{loadError}</p>
  }

  if (!clientSecret) {
    return (
      <div className="flex justify-center py-10">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <Elements stripe={stripePromise} options={{ clientSecret }}>
      <StripeConfirmForm onConfirmed={onConfirmed} />
    </Elements>
  )
}

interface CarwashKioskPaymentStepProps {
  slug: string | undefined
  quote: CarwashPaymentIntentRequest
  confirmedPayment: ConfirmedCarwashPayment | null
  onConfirmed: (payment: ConfirmedCarwashPayment) => void
  onAmountResolved: (amount: number) => void
  /** Se llama si el cobro en línea no se puede ofrecer, para que la pasarela deje pagar en el mostrador. */
  onUnavailable: () => void
}

/**
 * Paso de pago de la pasarela pública de Carwash: cobra servicio + agregados +
 * propina con Stripe (sandbox) y confirma contra el proveedor.
 *
 * Si el negocio no tiene Stripe configurado esto no bloquea el turno: avisa
 * hacia arriba y el cliente sigue, pagando en el mostrador como siempre. Un
 * portal que se cae porque faltan credenciales sería peor que no tener pago.
 */
export function CarwashKioskPaymentStep({
  slug,
  quote,
  confirmedPayment,
  onConfirmed,
  onAmountResolved,
  onUnavailable,
}: CarwashKioskPaymentStepProps) {
  const { t } = useTranslation("carwash")
  const { data: config, isLoading, isError } = useCarwashPublicPaymentConfig(slug)

  const publishableKey = config?.stripePublishableKey ?? null
  const unavailable = isError || (!isLoading && !publishableKey)

  useEffect(() => {
    if (unavailable) onUnavailable()
  }, [unavailable, onUnavailable])

  if (isLoading) {
    return (
      <div className="flex justify-center py-10">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (confirmedPayment) {
    return (
      <div className="flex items-center gap-3 rounded-xl border border-primary/30 bg-primary/5 p-4 text-sm">
        <CheckCircle2 className="h-5 w-5 shrink-0 text-primary" />
        <span>{t("kiosk.payment.confirmed")}</span>
      </div>
    )
  }

  if (unavailable || !publishableKey) {
    return <p className="py-4 text-sm text-muted-foreground">{t("kiosk.payment.unavailable")}</p>
  }

  return (
    <div className="space-y-4">
      <div className="flex items-start gap-2 rounded-xl border border-border/60 bg-muted/40 p-3 text-xs text-muted-foreground">
        <FlaskConical className="mt-0.5 h-4 w-4 shrink-0" />
        <span>{t("kiosk.payment.sandboxBanner")}</span>
      </div>
      <StripePanel
        slug={slug}
        publishableKey={publishableKey}
        quote={quote}
        onConfirmed={onConfirmed}
        onAmountResolved={onAmountResolved}
      />
    </div>
  )
}
