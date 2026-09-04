import { useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Link } from "react-router-dom"
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

function buildForgotPasswordFormSchema(t: TFunction) {
  return z.object({
    email: z
      .string()
      .trim()
      .min(1, t("forgotPassword.validation.emailRequired"))
      .email(t("forgotPassword.validation.emailInvalid")),
  })
}

type ForgotPasswordFormValues = z.infer<ReturnType<typeof buildForgotPasswordFormSchema>>

/**
 * Pide el link de recuperación de contraseña. Siempre muestra el mismo mensaje de
 * éxito, exista o no una cuenta con ese email: el backend nunca revela esa
 * información (ver AuthService.ForgotPasswordAsync), así que el frontend tampoco.
 */
export function ForgotPasswordPage() {
  const { t } = useTranslation("auth")
  const { forgotPassword } = useAuth()
  const [submitted, setSubmitted] = useState(false)

  const formSchema = useMemo(() => buildForgotPasswordFormSchema(t), [t])

  const form = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { email: "" },
  })

  async function onSubmit(values: ForgotPasswordFormValues) {
    try {
      await forgotPassword(values.email)
    } finally {
      // Siempre mostramos éxito, incluso si el request falla por red: no queremos
      // que un error de infraestructura termine revelando nada sobre el email.
      setSubmitted(true)
    }
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
              {t("forgotPassword.title")}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">{t("forgotPassword.subtitle")}</p>
          </div>
        </div>

        {submitted ? (
          <p className="rounded-lg border border-border bg-muted/40 p-4 text-center text-sm text-muted-foreground">
            {t("forgotPassword.successMessage")}
          </p>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("forgotPassword.emailLabel")}</FormLabel>
                    <FormControl>
                      <Input
                        type="email"
                        placeholder={t("forgotPassword.emailPlaceholder")}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <Button
                type="submit"
                className="w-full"
                size="lg"
                disabled={form.formState.isSubmitting}
              >
                {form.formState.isSubmitting
                  ? t("forgotPassword.submitting")
                  : t("forgotPassword.submit")}
              </Button>
            </form>
          </Form>
        )}

        <p className="text-center text-sm text-muted-foreground">
          <Link to="/login" className="font-medium text-primary hover:underline">
            {t("forgotPassword.backToLogin")}
          </Link>
        </p>
      </div>
    </div>
  )
}
