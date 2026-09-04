import { useEffect, useMemo, useState } from "react"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import { CheckCircle2, FlaskConical, Loader2 } from "lucide-react"
import { loadStripe } from "@stripe/stripe-js"
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js"
import { Button } from "@/components/ui/button"
import { usePortalPaymentConfig } from "@/application/portal/usePortalPaymentConfig"
import { useCreateStripeIntent } from "@/application/portal/useCreateStripeIntent"
import type { ConfirmedPortalPayment, PortalPaymentQuoteRequest } from "@/domain/types/portalPayment"

function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error) && typeof error.response?.data?.detail === "string") {
    return error.response.data.detail
  }
  return fallback
}

interface StripeConfirmFormProps {
  onConfirmed: (payment: ConfirmedPortalPayment) => void
}

/** Vive DENTRO de `<Elements>`: los hooks `useStripe`/`useElements` no funcionan fuera del provider. */
function StripeConfirmForm({ onConfirmed }: StripeConfirmFormProps) {
  const { t } = useTranslation("portal")
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
      setError(confirmError.message ?? t("checkout.payment.stripe.genericError"))
      return
    }
    if (paymentIntent?.status === "succeeded") {
      onConfirmed({ provider: "Stripe", reference: paymentIntent.id })
    } else {
      setError(t("checkout.payment.stripe.genericError"))
    }
  }

  return (
    <div className="space-y-3 pt-3">
      <PaymentElement />
      <p className="text-xs text-muted-foreground">
        {t("checkout.payment.stripe.testCardHint", { cardNumber: "4242 4242 4242 4242" })}
      </p>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button
        type="button"
        onClick={handleConfirm}
        disabled={!stripe || !elements || isConfirming}
        className="w-full"
      >
        {isConfirming ? t("checkout.payment.stripe.confirming") : t("checkout.payment.stripe.confirmButton")}
      </Button>
    </div>
  )
}

interface StripePaymentPanelProps {
  slug: string | undefined
  publishableKey: string
  quote: PortalPaymentQuoteRequest
  onConfirmed: (payment: ConfirmedPortalPayment) => void
}

function StripePaymentPanel({ slug, publishableKey, quote, onConfirmed }: StripePaymentPanelProps) {
  const { t } = useTranslation("portal")
  const stripePromise = useMemo(() => loadStripe(publishableKey), [publishableKey])
  const { mutate: createIntent } = useCreateStripeIntent(slug)
  const [clientSecret, setClientSecret] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setClientSecret(null)
    setLoadError(null)
    createIntent(quote, {
      onSuccess: (data) => {
        if (!cancelled) setClientSecret(data.clientSecret)
      },
      onError: (error) => {
        if (!cancelled) {
          setLoadError(extractErrorMessage(error, t("checkout.payment.stripe.genericError")))
        }
      },
    })
    return () => {
      cancelled = true
    }
  }, [createIntent, quote.assetId, quote.periods, quote.quantity, t])

  if (loadError) {
    return <p className="py-4 text-sm text-destructive">{loadError}</p>
  }

  if (!clientSecret) {
    return (
      <div className="flex justify-center py-6">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <Elements stripe={stripePromise} options={{ clientSecret }}>
      <StripeConfirmForm onConfirmed={onConfirmed} />
    </Elements>
  )
}

interface PortalPaymentStepProps {
  slug: string | undefined
  quote: PortalPaymentQuoteRequest
  confirmedPayment: ConfirmedPortalPayment | null
  onConfirmed: (payment: ConfirmedPortalPayment) => void
  onReset: () => void
}

/**
 * Paso 2 del wizard del Portal: pago con Stripe en modo sandbox y confirmación real contra
 * el proveedor. No se procesa ningún cargo productivo.
 */
export function PortalPaymentStep({
  slug,
  quote,
  confirmedPayment,
  onConfirmed,
  onReset,
}: PortalPaymentStepProps) {
  const { t } = useTranslation("portal")
  const { data: config, isLoading: isConfigLoading, isError: isConfigError } = usePortalPaymentConfig(slug)

  const stripeAvailable = !!config?.stripePublishableKey

  if (isConfigLoading) {
    return (
      <div className="flex justify-center py-8">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (confirmedPayment) {
    return (
      <div className="space-y-3">
        <div className="flex items-center gap-2 rounded-lg border border-primary/30 bg-primary/5 p-3 text-sm text-foreground">
          <CheckCircle2 className="h-4 w-4 shrink-0 text-primary" />
          <span>{t("checkout.payment.confirmed.stripe")}</span>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={onReset}>
          {t("checkout.payment.confirmed.changeMethod")}
        </Button>
      </div>
    )
  }

  if (isConfigError || !stripeAvailable) {
    return <p className="text-sm text-muted-foreground">{t("checkout.payment.bothUnavailable")}</p>
  }

  return (
    <div className="space-y-4">
      <div className="flex items-start gap-2 rounded-lg border border-border/60 bg-muted/40 p-3 text-xs text-muted-foreground">
        <FlaskConical className="mt-0.5 h-4 w-4 shrink-0" />
        <span>{t("checkout.payment.sandboxBanner")}</span>
      </div>

      {config ? (
        <StripePaymentPanel
          key={`${quote.assetId}-${quote.periods}-${quote.quantity}`}
          slug={slug}
          publishableKey={config.stripePublishableKey}
          quote={quote}
          onConfirmed={onConfirmed}
        />
      ) : (
        <p className="py-4 text-sm text-muted-foreground">{t("checkout.payment.stripe.unavailable")}</p>
      )}
    </div>
  )
}
