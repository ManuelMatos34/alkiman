import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useMe } from "@/application/me/useMe"
import { useUpdateMe } from "@/application/me/useUpdateMe"
import { useChangeMyPassword } from "@/application/me/useChangeMyPassword"

function buildIdentityFormSchema(t: TFunction) {
  return z.object({
    fullName: z.string().trim().min(1, t("myAccount.validation.fullNameRequired")).max(150),
    email: z.string().trim().email(t("myAccount.validation.emailInvalid")),
  })
}

type IdentityFormValues = z.infer<ReturnType<typeof buildIdentityFormSchema>>

function buildPasswordFormSchema(t: TFunction) {
  return z
    .object({
      currentPassword: z.string().min(1, t("myAccount.validation.currentPasswordRequired")),
      newPassword: z.string().min(8, t("myAccount.validation.passwordMin")),
      confirmPassword: z.string().min(1, t("myAccount.validation.confirmPasswordRequired")),
    })
    .refine((values) => values.newPassword === values.confirmPassword, {
      message: t("myAccount.validation.passwordMismatch"),
      path: ["confirmPassword"],
    })
}

type PasswordFormValues = z.infer<ReturnType<typeof buildPasswordFormSchema>>

/** Datos de la cuenta propia del usuario autenticado (identidad y contraseña). */
export function MyAccountSettingsForm() {
  const { t } = useTranslation(["settingsForms", "common"])
  const { data: me } = useMe()
  const updateMe = useUpdateMe()
  const changePassword = useChangeMyPassword()

  const identityFormSchema = useMemo(() => buildIdentityFormSchema(t), [t])
  const passwordFormSchema = useMemo(() => buildPasswordFormSchema(t), [t])

  const identityForm = useForm<IdentityFormValues>({
    resolver: zodResolver(identityFormSchema),
    defaultValues: { fullName: "", email: "" },
  })

  const passwordForm = useForm<PasswordFormValues>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  })

  useEffect(() => {
    if (!me) return
    identityForm.reset({ fullName: me.fullName, email: me.email })
  }, [me, identityForm])

  function onSubmitIdentity(values: IdentityFormValues) {
    updateMe.mutate(values, {
      onSuccess: () => toast.success(t("myAccount.toastIdentitySuccess")),
      onError: (error) => {
        if (isAxiosError(error) && error.response?.status === 409) {
          toast.error(t("myAccount.toastEmailTaken"))
        } else {
          toast.error(t("myAccount.toastIdentityError"))
        }
      },
    })
  }

  function onSubmitPassword(values: PasswordFormValues) {
    changePassword.mutate(
      { currentPassword: values.currentPassword, newPassword: values.newPassword },
      {
        onSuccess: () => {
          toast.success(t("myAccount.toastPasswordSuccess"))
          passwordForm.reset({ currentPassword: "", newPassword: "", confirmPassword: "" })
        },
        onError: (error) => {
          if (isAxiosError(error) && error.response?.status === 400) {
            toast.error(t("myAccount.toastCurrentPasswordWrong"))
          } else {
            toast.error(t("myAccount.toastPasswordError"))
          }
        },
      }
    )
  }

  return (
    <div className="space-y-6">
      <Card>
        <Form {...identityForm}>
          <form onSubmit={identityForm.handleSubmit(onSubmitIdentity)}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>{t("myAccount.identityCardTitle")}</CardTitle>
                  <CardDescription>{t("myAccount.identityCardDescription")}</CardDescription>
                </div>
                {me && <Badge variant="secondary">{me.role}</Badge>}
              </div>
            </CardHeader>
            <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <FormField
                control={identityForm.control}
                name="fullName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("myAccount.fullNameLabel")}</FormLabel>
                    <FormControl>
                      <Input placeholder={t("myAccount.fullNamePlaceholder")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={identityForm.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("myAccount.emailLabel")}</FormLabel>
                    <FormControl>
                      <Input type="email" placeholder={t("myAccount.emailPlaceholder")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CardContent>
            <CardFooter className="justify-end">
              <Button type="submit" disabled={updateMe.isPending}>
                {updateMe.isPending ? t("common:status.saving") : t("myAccount.saveButton")}
              </Button>
            </CardFooter>
          </form>
        </Form>
      </Card>

      <Card>
        <Form {...passwordForm}>
          <form onSubmit={passwordForm.handleSubmit(onSubmitPassword)}>
            <CardHeader>
              <CardTitle>{t("myAccount.passwordCardTitle")}</CardTitle>
              <CardDescription>
                {t("myAccount.passwordCardDescription")}
              </CardDescription>
            </CardHeader>
            <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <FormField
                control={passwordForm.control}
                name="currentPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("myAccount.currentPasswordLabel")}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder={t("myAccount.passwordPlaceholder")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={passwordForm.control}
                name="newPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("myAccount.newPasswordLabel")}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder={t("myAccount.passwordPlaceholder")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={passwordForm.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t("myAccount.confirmPasswordLabel")}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder={t("myAccount.passwordPlaceholder")} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CardContent>
            <CardFooter className="justify-end">
              <Button type="submit" disabled={changePassword.isPending}>
                {changePassword.isPending ? t("common:status.saving") : t("myAccount.changePasswordButton")}
              </Button>
            </CardFooter>
          </form>
        </Form>
      </Card>
    </div>
  )
}
