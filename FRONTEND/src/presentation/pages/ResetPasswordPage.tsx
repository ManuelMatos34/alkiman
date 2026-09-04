import { useMemo, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import { toast } from "sonner"
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

function buildResetPasswordFormSchema(t: TFunction) {
  return z
    .object({
      newPassword: z.string().min(8, t("resetPassword.validation.passwordMin")),
      confirmPassword: z.string().min(1, t("resetPassword.validation.confirmPasswordRequired")),
    })
    .refine((values) => values.newPassword === values.confirmPassword, {
      message: t("resetPassword.validation.passwordMismatch"),
      path: ["confirmPassword"],
    })
}

type ResetPasswordFormValues = z.infer<ReturnType<typeof buildResetPasswordFormSchema>>

/** Confirma la recuperación de contraseña con el token recibido por email (?token=...). */
export function ResetPasswordPage() {
  const { t } = useTranslation("auth")
  const { resetPassword } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get("token") ?? ""
  const [formError, setFormError] = useState<string | null>(null)

  const formSchema = useMemo(() => buildResetPasswordFormSchema(t), [t])

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  })

  async function onSubmit(values: ResetPasswordFormValues) {
    setFormError(null)
    try {
      await resetPassword(token, values.newPassword)
      toast.success(t("resetPassword.toastSuccess"))
      navigate("/login", { replace: true })
    } catch {
      setFormError(t("resetPassword.errorGeneric"))
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
              {t("resetPassword.title")}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">{t("resetPassword.subtitle")}</p>
          </div>
        </div>

        {!token ? (
          <p className="rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-center text-sm text-destructive">
            {t("resetPassword.missingToken")}
          </p>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="newPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("resetPassword.newPasswordLabel")}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="••••••••" {...field} />
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
                    <FormLabel>{t("resetPassword.confirmPasswordLabel")}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="••••••••" {...field} />
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
                {form.formState.isSubmitting
                  ? t("resetPassword.submitting")
                  : t("resetPassword.submit")}
              </Button>
            </form>
          </Form>
        )}

        <p className="text-center text-sm text-muted-foreground">
          <Link to="/login" className="font-medium text-primary hover:underline">
            {t("resetPassword.backToLogin")}
          </Link>
        </p>
      </div>
    </div>
  )
}
