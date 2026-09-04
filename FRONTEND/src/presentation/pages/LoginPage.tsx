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

type LoginFormValues = z.infer<ReturnType<typeof buildLoginFormSchema>>

export function LoginPage() {
  const { t } = useTranslation("auth")
  const { login, isAuthenticated, isLoading } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const loginFormSchema = useMemo(() => buildLoginFormSchema(t), [t])

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: { email: "", password: "" },
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
      await login(values.email, values.password)
      navigate("/", { replace: true })
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 401) {
        setFormError(t("login.errorInvalidCredentials"))
      } else {
        setFormError(t("login.errorGeneric"))
      }
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
            <h1 className="text-2xl font-semibold tracking-tight">{t("login.appName")}</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {t("login.tagline")}
            </p>
          </div>
        </div>

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
                    <Input type="password" placeholder={t("login.passwordPlaceholder")} {...field} />
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
      </div>
    </div>
  )
}
