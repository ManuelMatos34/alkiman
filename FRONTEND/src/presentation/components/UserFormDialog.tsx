import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { isAxiosError } from "axios"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { useCreateUser } from "@/application/users/useCreateUser"
import { useUpdateUser } from "@/application/users/useUpdateUser"
import { useRoles } from "@/application/roles/useRoles"
import type { User } from "@/domain/types/user"

function buildUserFormSchema(t: TFunction) {
  return z.object({
    fullName: z
      .string()
      .trim()
      .min(1, t("validation.fullNameRequired"))
      .max(150, t("validation.fullNameMax")),
    email: z
      .string()
      .trim()
      .min(1, t("validation.emailRequired"))
      .email(t("validation.emailInvalid")),
    roleId: z.string().min(1, t("validation.roleRequired")),
    isActive: z.boolean(),
  })
}

type UserFormValues = z.infer<ReturnType<typeof buildUserFormSchema>>

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user?: User | null
}

/** Diálogo de alta/edición de usuarios del negocio. */
export function UserFormDialog({ open, onOpenChange, user }: UserFormDialogProps) {
  const { t } = useTranslation(["users", "common"])
  const isEditing = !!user
  const { data: roles } = useRoles()
  const createUser = useCreateUser()
  const updateUser = useUpdateUser()

  const userFormSchema = useMemo(() => buildUserFormSchema(t), [t])

  const form = useForm<UserFormValues>({
    resolver: zodResolver(userFormSchema),
    defaultValues: {
      fullName: "",
      email: "",
      roleId: "",
      isActive: true,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        fullName: user?.fullName ?? "",
        email: user?.email ?? "",
        roleId: user ? String(user.roleId) : "",
        isActive: user?.isActive ?? true,
      })
    }
  }, [open, user, form])

  function onSubmit(values: UserFormValues) {
    if (user) {
      updateUser.mutate(
        {
          id: user.id,
          request: {
            fullName: values.fullName,
            roleId: Number(values.roleId),
            isActive: values.isActive,
          },
        },
        {
          onSuccess: () => {
            toast.success(t("toast.updateSuccess"))
            onOpenChange(false)
          },
          onError: () => {
            toast.error(t("toast.updateError"))
          },
        }
      )
      return
    }

    createUser.mutate(
      {
        fullName: values.fullName,
        email: values.email,
        roleId: Number(values.roleId),
      },
      {
        onSuccess: (created) => {
          toast.success(t("toast.createSuccess"))
          onOpenChange(false)
          // La contraseña temporal nunca llega al front: sólo la recibe su dueño por
          // email. Si ese envío falló el usuario no puede entrar, así que hay que
          // avisarle al admin para que lo derive a "olvidé mi contraseña".
          if (!created.welcomeEmailSent) {
            toast.warning(t("toast.createWelcomeEmailFailed"), { duration: 10000 })
          }
        },
        onError: (error) => {
          if (isAxiosError(error) && error.response?.status === 409) {
            toast.error(t("toast.createErrorDuplicate"))
          } else {
            toast.error(t("toast.createError"))
          }
        },
      }
    )
  }

  const isPending = createUser.isPending || updateUser.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.titleEdit") : t("formDialog.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="fullName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.fullName")}</FormLabel>
                  <FormControl>
                    <Input placeholder={t("formDialog.placeholders.fullName")} {...field} />
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
                  <FormLabel>{t("formDialog.fields.email")}</FormLabel>
                  <FormControl>
                    <Input
                      type="email"
                      placeholder={t("formDialog.placeholders.email")}
                      disabled={isEditing}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {!isEditing && (
              <p className="text-sm text-muted-foreground">
                {t("formDialog.generatedPasswordNote")}
              </p>
            )}

            <FormField
              control={form.control}
              name="roleId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.role")}</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange} disabled={isEditing && user?.isOwner}>
                    <FormControl>
                      <SelectTrigger className="w-full" disabled={isEditing && user?.isOwner}>
                        <SelectValue placeholder={t("formDialog.placeholders.role")} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {roles?.map((role) => (
                        <SelectItem key={role.id} value={String(role.id)}>
                          {role.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isEditing && user?.isOwner && (
              <p className="text-sm text-muted-foreground">{t("formDialog.ownerNote")}</p>
            )}

            {isEditing && !user?.isOwner && (
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border border-border/60 p-3">
                    <FormLabel className="mb-0">{t("formDialog.fields.active")}</FormLabel>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                  </FormItem>
                )}
              />
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
