import { useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useNavigate } from "react-router-dom"
import { isAxiosError } from "axios"
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
import { useChangeMyPassword } from "@/application/me/useChangeMyPassword"

function buildForcedChangePasswordFormSchema(t: TFunction) {
  return z
    .object({
      currentPassword: z.string().min(1, t("forcedChangePassword.validation.currentPasswordRequired")),
      newPassword: z.string().min(8, t("forcedChangePassword.validation.passwordMin")),
      confirmPassword: z.string().min(1, t("forcedChangePassword.validation.confirmPasswordRequired")),
    })
    .refine((values) => values.newPassword === values.confirmPassword, {
      message: t("forcedChangePassword.validation.passwordMismatch"),
      path: ["confirmPassword"],
    })
}

type ForcedChangePasswordFormValues = z.infer<ReturnType<typeof buildForcedChangePasswordFormSchema>>

/**
 * Pantalla obligatoria para usuarios con una contraseña generada/temporal pendiente
 * (MustChangePassword): no se puede navegar al resto de la app hasta cambiarla
 * (ver ProtectedRoute). Reutiliza el mismo endpoint de autoservicio que "Seguridad"
 * (PUT /api/me/password) y, al tener éxito, apaga el flag localmente sin requerir
 * un nuevo login.
 */
export function ForcedChangePasswordPage() {
  const { t } = useTranslation("auth")
  const { user, updateMustChangePassword, logout } = useAuth()
  const navigate = useNavigate()
  const changePassword = useChangeMyPassword()

  const formSchema = useMemo(() => buildForcedChangePasswordFormSchema(t), [t])

  const form = useForm<ForcedChangePasswordFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  })

  function onSubmit(values: ForcedChangePasswordFormValues) {
    changePassword.mutate(
      { currentPassword: values.currentPassword, newPassword: values.newPassword },
      {
        onSuccess: () => {
          toast.success(t("forcedChangePassword.toastSuccess"))
          updateMustChangePassword(false)
          navigate("/", { replace: true })
        },
        onError: (error) => {
          if (isAxiosError(error) && error.response?.status === 400) {
            form.setError("currentPassword", {
              message: t("forcedChangePassword.errorCurrentPassword"),
            })
          } else {
            toast.error(t("forcedChangePassword.errorGeneric"))
          }
        },
      }
    )
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
              {t("forcedChangePassword.title")}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {user
                ? t("forcedChangePassword.subtitleWithName", { name: user.fullName })
                : t("forcedChangePassword.subtitle")}
            </p>
          </div>
        </div>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="currentPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("forcedChangePassword.currentPasswordLabel")}</FormLabel>
                  <FormControl>
                    <Input type="password" placeholder="••••••••" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="newPassword"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("forcedChangePassword.newPasswordLabel")}</FormLabel>
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
                  <FormLabel>{t("forcedChangePassword.confirmPasswordLabel")}</FormLabel>
                  <FormControl>
                    <Input type="password" placeholder="••••••••" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <Button
              type="submit"
              className="w-full"
              size="lg"
              disabled={form.formState.isSubmitting || changePassword.isPending}
            >
              {changePassword.isPending
                ? t("forcedChangePassword.submitting")
                : t("forcedChangePassword.submit")}
            </Button>
          </form>
        </Form>

        <p className="text-center text-sm text-muted-foreground">
          <button
            type="button"
            onClick={() => logout()}
            className="font-medium text-primary hover:underline"
          >
            {t("forcedChangePassword.logoutLink")}
          </button>
        </p>
      </div>
    </div>
  )
}
