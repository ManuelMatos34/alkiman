import { useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Link, Navigate, useNavigate } from "react-router-dom"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useAuth } from "@/infrastructure/auth/AuthContext"
import { FullScreenLoader } from "@/presentation/components/FullScreenLoader"

function buildLoginFormSchema(t: TFunction) {
  return z.object({
    email: z
      .string()
      .trim()
      .min(1, t("login.validation.emailRequired"))
      .email(t("login.validation.emailInvalid")),
    password: z.string().min(1, t("login.validation.passwordRequired")),
  })
}

function buildTwoFactorFormSchema(t: TFunction) {
  return z.object({
    code: z
      .string()
      .trim()
      .regex(/^\d{6}$/, t("twoFactor.validation.codeFormat")),
  })
}

type LoginFormValues = z.infer<ReturnType<typeof buildLoginFormSchema>>
type TwoFactorFormValues = z.infer<ReturnType<typeof buildTwoFactorFormSchema>>

/** Desafío de doble factor en curso: el token que canjear y a qué correo fue el código. */
interface PendingChallenge {
  token: string
  email: string
}

/**
 * Extrae el mensaje que la API escribió para el usuario. El backend devuelve
 * ProblemDetails con `detail` redactado para leerse tal cual ("Te quedan 3 intentos"),
 * y perderlo obligaría a mostrar un genérico mucho menos útil.
 */
function readApiMessage(error: unknown): string | null {
  if (!isAxiosError(error)) return null
  const detail = (error.response?.data as { detail?: unknown } | undefined)?.detail
  return typeof detail === "string" && detail.trim().length > 0 ? detail : null
}

export function LoginPage() {
  const { t } = useTranslation("auth")
  const { login, verifyTwoFactor, resendTwoFactorCode, isAuthenticated, isLoading } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [challenge, setChallenge] = useState<PendingChallenge | null>(null)
  const [resendNotice, setResendNotice] = useState<string | null>(null)
  const [isResending, setIsResending] = useState(false)

  const loginFormSchema = useMemo(() => buildLoginFormSchema(t), [t])
  const twoFactorFormSchema = useMemo(() => buildTwoFactorFormSchema(t), [t])

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: { email: "", password: "" },
  })

  const codeForm = useForm<TwoFactorFormValues>({
    resolver: zodResolver(twoFactorFormSchema),
    defaultValues: { code: "" },
  })

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function onSubmit(values: LoginFormValues) {
    setFormError(null)
    try {
      const outcome = await login(values.email, values.password)
      if (outcome.requiresTwoFactor) {
        setChallenge({ token: outcome.challengeToken, email: values.email })
        return
      }
      navigate("/", { replace: true })
    } catch (error) {
      // El mensaje de la API ya distingue credenciales malas de usuario desactivado
      // o de un fallo al mandar el código; el genérico queda sólo para caídas de red.
      setFormError(readApiMessage(error) ?? t("login.errorGeneric"))
    }
  }

  async function onSubmitCode(values: TwoFactorFormValues) {
    if (!challenge) return
    setFormError(null)
    setResendNotice(null)
    try {
      await verifyTwoFactor(challenge.token, values.code)
      navigate("/", { replace: true })
    } catch (error) {
      setFormError(readApiMessage(error) ?? t("twoFactor.errorGeneric"))
      codeForm.reset({ code: "" })
    }
  }

  async function onResend() {
    if (!challenge) return
    setFormError(null)
    setResendNotice(null)
    setIsResending(true)
    try {
      await resendTwoFactorCode(challenge.token)
      setResendNotice(t("twoFactor.resendSuccess"))
    } catch (error) {
      setFormError(readApiMessage(error) ?? t("twoFactor.errorGeneric"))
    } finally {
      setIsResending(false)
    }
  }

  /** Vuelve al paso de credenciales. El desafío del servidor vence solo. */
  function onCancelChallenge() {
    setChallenge(null)
    setFormError(null)
    setResendNotice(null)
    codeForm.reset({ code: "" })
    form.resetField("password")
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-sm space-y-8">
        <div className="space-y-3 text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-lg font-semibold text-primary-foreground">
            A
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">
              {challenge ? t("twoFactor.title") : t("login.appName")}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {challenge
                ? t("twoFactor.subtitle", { email: challenge.email })
                : t("login.tagline")}
            </p>
          </div>
        </div>

        {challenge ? (
          <Form {...codeForm}>
            <form onSubmit={codeForm.handleSubmit(onSubmitCode)} className="space-y-4">
              <FormField
                control={codeForm.control}
                name="code"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("twoFactor.codeLabel")}</FormLabel>
                    <FormControl>
                      <Input
                        // inputMode numérico + autocomplete para que el gestor del
                        // teléfono ofrezca el código apenas llega el correo.
                        inputMode="numeric"
                        autoComplete="one-time-code"
                        maxLength={6}
                        autoFocus
                        placeholder="000000"
                        className="text-center text-2xl tracking-[0.5em] tabular-nums"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {formError && <p className="text-sm text-destructive">{formError}</p>}
              {resendNotice && <p className="text-sm text-muted-foreground">{resendNotice}</p>}

              <Button
                type="submit"
                className="w-full"
                size="lg"
                disabled={codeForm.formState.isSubmitting}
              >
                {codeForm.formState.isSubmitting
                  ? t("twoFactor.submitting")
                  : t("twoFactor.submit")}
              </Button>

              <div className="flex items-center justify-between text-sm">
                <button
                  type="button"
                  onClick={onCancelChallenge}
                  className="font-medium text-muted-foreground hover:underline"
                >
                  {t("twoFactor.back")}
                </button>
                <button
                  type="button"
                  onClick={onResend}
                  disabled={isResending}
                  className="font-medium text-primary hover:underline disabled:opacity-60"
                >
                  {isResending ? t("twoFactor.resending") : t("twoFactor.resend")}
                </button>
              </div>
            </form>
          </Form>
        ) : (
          <>
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("login.emailLabel")}</FormLabel>
                      <FormControl>
                        <Input type="email" placeholder={t("login.emailPlaceholder")} {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="password"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t("login.passwordLabel")}</FormLabel>
                      <FormControl>
                        <Input
                          type="password"
                          placeholder={t("login.passwordPlaceholder")}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                {formError && <p className="text-sm text-destructive">{formError}</p>}

                <div className="text-right">
                  <Link
                    to="/olvide-password"
                    className="text-sm font-medium text-primary hover:underline"
                  >
                    {t("login.forgotPasswordLink")}
                  </Link>
                </div>

                <Button
                  type="submit"
                  className="w-full"
                  size="lg"
                  disabled={form.formState.isSubmitting}
                >
                  {form.formState.isSubmitting ? t("login.submitting") : t("login.submit")}
                </Button>
              </form>
            </Form>

            <p className="text-center text-sm text-muted-foreground">
              {t("login.noAccount")}{" "}
              <Link to="/register" className="font-medium text-primary hover:underline">
                {t("login.registerLink")}
              </Link>
            </p>
          </>
        )}
      </div>
    </div>
  )
}
