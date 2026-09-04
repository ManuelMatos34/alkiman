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

function buildRegisterFormSchema(t: TFunction) {
  return z
    .object({
      businessName: z
        .string()
        .trim()
        .min(1, t("register.validation.businessNameRequired"))
        .max(150, t("register.validation.maxChars")),
      fullName: z
        .string()
        .trim()
        .min(1, t("register.validation.fullNameRequired"))
        .max(150, t("register.validation.maxChars")),
      email: z
        .string()
        .trim()
        .min(1, t("register.validation.emailRequired"))
        .email(t("register.validation.emailInvalid")),
      password: z.string().min(8, t("register.validation.passwordMin")),
      confirmPassword: z.string().min(1, t("register.validation.confirmPasswordRequired")),
    })
    .refine((values) => values.password === values.confirmPassword, {
      message: t("register.validation.passwordMismatch"),
      path: ["confirmPassword"],
    })
}

type RegisterFormValues = z.infer<ReturnType<typeof buildRegisterFormSchema>>

export function RegisterPage() {
  const { t } = useTranslation("auth")
  const { register, isAuthenticated, isLoading } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const registerFormSchema = useMemo(() => buildRegisterFormSchema(t), [t])

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerFormSchema),
    defaultValues: {
      businessName: "",
      fullName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
  })

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function onSubmit(values: RegisterFormValues) {
    setFormError(null)
    try {
      await register(values.businessName, values.fullName, values.email, values.password)
      navigate("/", { replace: true })
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 409) {
        setFormError(t("register.errorEmailTaken"))
      } else {
        setFormError(t("register.errorGeneric"))
      }
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-12">
      <div className="w-full max-w-sm space-y-8">
        <div className="space-y-3 text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-lg font-semibold text-primary-foreground">
            A
          </div>
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">{t("register.title")}</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {t("register.subtitle")}
            </p>
          </div>
        </div>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="businessName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("register.businessNameLabel")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("register.businessNamePlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="fullName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("register.fullNameLabel")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("register.fullNamePlaceholder")} {...field} />
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
                  <FormLabel>{t("register.emailLabel")}</FormLabel>
                  <FormControl>
                    <Input type="email" placeholder={t("register.emailPlaceholder")} {...field} />
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
                  <FormLabel>{t("register.passwordLabel")}</FormLabel>
                  <FormControl>
                    <Input type="password" placeholder={t("register.passwordPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="confirmPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("register.confirmPasswordLabel")}</FormLabel>
                  <FormControl>
                    <Input type="password" placeholder={t("register.passwordPlaceholder")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {formError && <p className="text-sm text-destructive">{formError}</p>}

            <Button
              type="submit"
              className="w-full"
              size="lg"
              disabled={form.formState.isSubmitting}
            >
              {form.formState.isSubmitting ? t("register.submitting") : t("register.submit")}
            </Button>
          </form>
        </Form>

        <p className="text-center text-sm text-muted-foreground">
          {t("register.haveAccount")}{" "}
          <Link to="/login" className="font-medium text-primary hover:underline">
            {t("register.loginLink")}
          </Link>
        </p>
      </div>
    </div>
  )
}
