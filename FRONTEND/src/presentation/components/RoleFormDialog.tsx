import { useEffect, useMemo } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import type { TFunction } from "i18next"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"
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
import { useCreateRole } from "@/application/roles/useCreateRole"
import { useUpdateRole } from "@/application/roles/useUpdateRole"
import { usePermissions } from "@/application/permissions/usePermissions"
import type { Role } from "@/domain/types/role"

function buildRoleFormSchema(t: TFunction) {
  return z.object({
    name: z.string().trim().min(1, t("validation.nameRequired")).max(100, t("validation.nameMax")),
    description: z.string().trim().max(300, t("validation.descriptionMax")).optional(),
    permissions: z.array(z.string()).min(1, t("validation.permissionsRequired")),
  })
}

type RoleFormValues = z.infer<ReturnType<typeof buildRoleFormSchema>>

interface RoleFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  role?: Role | null
}

/** Diálogo de alta/edición de roles y sus permisos. */
export function RoleFormDialog({ open, onOpenChange, role }: RoleFormDialogProps) {
  const { t } = useTranslation(["roles", "common"])
  const isEditing = !!role
  const isSystemRole = !!role?.isSystem
  const { data: permissions } = usePermissions()
  const createRole = useCreateRole()
  const updateRole = useUpdateRole()

  const roleFormSchema = useMemo(() => buildRoleFormSchema(t), [t])

  const groupedPermissions = useMemo(() => {
    const groups = new Map<string, typeof permissions>()
    for (const permission of permissions ?? []) {
      const list = groups.get(permission.module) ?? []
      list.push(permission)
      groups.set(permission.module, list)
    }
    return Array.from(groups.entries())
  }, [permissions])

  const form = useForm<RoleFormValues>({
    resolver: zodResolver(roleFormSchema),
    defaultValues: { name: "", description: "", permissions: [] },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        name: role?.name ?? "",
        description: role?.description ?? "",
        permissions: role?.permissions ?? [],
      })
    }
  }, [open, role, form])

  function onSubmit(values: RoleFormValues) {
    const payload = {
      name: values.name,
      description: values.description || null,
      permissions: values.permissions,
    }

    if (role) {
      updateRole.mutate(
        { id: role.id, request: payload },
        {
          onSuccess: () => {
            toast.success(t("toast.updateSuccess"))
            onOpenChange(false)
          },
          onError: () => toast.error(t("toast.updateError")),
        }
      )
      return
    }

    createRole.mutate(payload, {
      onSuccess: () => {
        toast.success(t("toast.createSuccess"))
        onOpenChange(false)
      },
      onError: () => toast.error(t("toast.createError")),
    })
  }

  const isPending = createRole.isPending || updateRole.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t("formDialog.titleEdit") : t("formDialog.titleNew")}
          </DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.name")}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t("formDialog.placeholders.name")}
                      disabled={isSystemRole}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.description")}</FormLabel>
                  <FormControl>
                    <Textarea placeholder={t("formDialog.placeholders.description")} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="permissions"
              render={() => (
                <FormItem>
                  <FormLabel>{t("formDialog.fields.permissions")}</FormLabel>
                  <div className="space-y-4 rounded-lg border border-border/60 p-3">
                    {groupedPermissions.map(([module, items]) => (
                      <div key={module} className="space-y-2">
                        <p className="text-xs font-semibold text-muted-foreground uppercase">
                          {module}
                        </p>
                        <div className="space-y-2">
                          {items?.map((permission) => (
                            <FormField
                              key={permission.code}
                              control={form.control}
                              name="permissions"
                              render={({ field }) => {
                                const checked = field.value.includes(permission.code)
                                return (
                                  <FormItem className="flex flex-row items-start gap-2 space-y-0">
                                    <FormControl>
                                      <Checkbox
                                        checked={checked}
                                        disabled={isSystemRole}
                                        onCheckedChange={(value) => {
                                          if (value) {
                                            field.onChange([...field.value, permission.code])
                                          } else {
                                            field.onChange(
                                              field.value.filter((c) => c !== permission.code)
                                            )
                                          }
                                        }}
                                      />
                                    </FormControl>
                                    <FormLabel className="text-sm font-normal">
                                      {permission.description}
                                    </FormLabel>
                                  </FormItem>
                                )
                              }}
                            />
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isSystemRole && (
              <p className="text-sm text-muted-foreground">{t("formDialog.systemNote")}</p>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t("common:buttons.cancel")}
              </Button>
              <Button type="submit" disabled={isPending || isSystemRole}>
                {isPending ? t("common:status.saving") : t("common:buttons.save")}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
