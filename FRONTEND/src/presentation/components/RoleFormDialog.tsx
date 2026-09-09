import { useEffect, useMemo, useRef, useState } from "react"
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
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
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
// FormControl sigue usándose en los campos de nombre y descripción.
import { useCreateRole } from "@/application/roles/useCreateRole"
import { useUpdateRole } from "@/application/roles/useUpdateRole"
import { usePermissions } from "@/application/permissions/usePermissions"
import { useModules } from "@/application/modules/useModules"
import type { Permission } from "@/domain/types/permission"
import type { Role } from "@/domain/types/role"

/**
 * Clave del grupo que junta los permisos sin módulo (usuarios, roles, ajustes,
 * bitácora...). No puede chocar con un código real de módulo porque los códigos
 * vienen de dbo.CFG_Modules y ninguno lleva guiones bajos dobles.
 */
const PLATFORM_GROUP_KEY = "__platform"

/** Un módulo del acordeón, con sus permisos ya partidos en subgrupos temáticos. */
interface PermissionGroup {
  key: string
  label: string
  /** Subgrupos por la etiqueta visual `permission.module` ("Activos", "Pagos"...). */
  subgroups: { label: string; items: Permission[] }[]
  /** Todos los códigos del módulo, para el "seleccionar todo" y el contador. */
  codes: string[]
}

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
  const { data: modules } = useModules()
  const createRole = useCreateRole()
  const updateRole = useUpdateRole()

  const roleFormSchema = useMemo(() => buildRoleFormSchema(t), [t])

  const permissionGroups = useMemo<PermissionGroup[]>(() => {
    // moduleCode agrupa arriba (Alquileres, Carwash, Plataforma) y module abajo
    // (Activos, Pagos...). Un Map de Maps preserva el orden de llegada, que es el
    // que ya trae ordenado el backend.
    const byModule = new Map<string, Map<string, Permission[]>>()
    for (const permission of permissions ?? []) {
      const key = permission.moduleCode ?? PLATFORM_GROUP_KEY
      let subgroups = byModule.get(key)
      if (!subgroups) {
        subgroups = new Map()
        byModule.set(key, subgroups)
      }
      const items = subgroups.get(permission.module) ?? []
      items.push(permission)
      subgroups.set(permission.module, items)
    }

    // Plataforma primero (aplica a cualquier rol), después los módulos en el
    // orden del catálogo. Lo que no esté en el catálogo cae al final en vez de
    // desaparecer: si algún día llega un permiso de un módulo desconocido,
    // preferimos mostrarlo mal ordenado antes que ocultarlo.
    const order = [PLATFORM_GROUP_KEY, ...(modules ?? []).map((m) => m.code)]
    const rank = (key: string) => {
      const index = order.indexOf(key)
      return index === -1 ? order.length : index
    }

    return Array.from(byModule.entries())
      .sort(([a], [b]) => rank(a) - rank(b))
      .map(([key, subgroups]) => ({
        key,
        label:
          key === PLATFORM_GROUP_KEY
            ? t("formDialog.platformGroup")
            : (modules?.find((m) => m.code === key)?.name ?? key),
        subgroups: Array.from(subgroups.entries()).map(([label, items]) => ({ label, items })),
        codes: Array.from(subgroups.values()).flatMap((items) => items.map((i) => i.code)),
      }))
  }, [permissions, modules, t])

  const form = useForm<RoleFormValues>({
    resolver: zodResolver(roleFormSchema),
    defaultValues: { name: "", description: "", permissions: [] },
  })

  const [openGroups, setOpenGroups] = useState<string[]>([])
  const didPresetOpenGroups = useRef(false)

  useEffect(() => {
    if (open) {
      form.reset({
        name: role?.name ?? "",
        description: role?.description ?? "",
        permissions: role?.permissions ?? [],
      })
    }
  }, [open, role, form])

  useEffect(() => {
    if (!open) {
      didPresetOpenGroups.current = false
      return
    }
    // Los permisos llegan por query, así que este efecto se dispara otra vez
    // cuando resuelve. El ref evita que un refetch posterior vuelva a abrir los
    // módulos que el usuario ya había plegado a mano.
    if (didPresetOpenGroups.current || permissionGroups.length === 0) return
    didPresetOpenGroups.current = true

    const assigned = new Set(role?.permissions ?? [])
    const withSelection = permissionGroups
      .filter((group) => group.codes.some((code) => assigned.has(code)))
      .map((group) => group.key)

    // Editando se abren los módulos que el rol ya toca; creando, sólo el primero,
    // para que el diálogo no arranque como la lista larga que veníamos evitando.
    setOpenGroups(withSelection.length > 0 ? withSelection : [permissionGroups[0].key])
  }, [open, role, permissionGroups])

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
              render={({ field }) => {
                const selected = new Set(field.value)

                const togglePermission = (code: string, on: boolean) =>
                  field.onChange(
                    on ? [...field.value, code] : field.value.filter((c) => c !== code)
                  )

                const toggleGroup = (codes: string[], on: boolean) => {
                  if (on) {
                    // Set para no duplicar lo que ya estaba marcado dentro del módulo.
                    const merged = new Set(field.value)
                    for (const code of codes) merged.add(code)
                    field.onChange(Array.from(merged))
                    return
                  }
                  const removing = new Set(codes)
                  field.onChange(field.value.filter((c) => !removing.has(c)))
                }

                return (
                  <FormItem>
                    <div className="flex items-baseline justify-between gap-2">
                      <FormLabel>{t("formDialog.fields.permissions")}</FormLabel>
                      <span className="text-xs text-muted-foreground">
                        {t("formDialog.permissionsCounter", {
                          selected: selected.size,
                          total: permissionGroups.reduce((n, g) => n + g.codes.length, 0),
                        })}
                      </span>
                    </div>

                    <div className="rounded-lg border border-border/60 px-3">
                      <Accordion
                        type="multiple"
                        value={openGroups}
                        onValueChange={setOpenGroups}
                      >
                        {permissionGroups.map((group) => {
                          const selectedInGroup = group.codes.filter((c) =>
                            selected.has(c)
                          ).length
                          const allSelected = selectedInGroup === group.codes.length
                          const groupState = allSelected
                            ? true
                            : selectedInGroup > 0
                              ? "indeterminate"
                              : false

                          return (
                            <AccordionItem key={group.key} value={group.key}>
                              {/* El "seleccionar todo" va fuera del trigger: Radix
                                  renderiza ambos como <button> y anidarlos sería
                                  HTML inválido. */}
                              <div className="flex items-center gap-3">
                                <Checkbox
                                  checked={groupState}
                                  disabled={isSystemRole}
                                  aria-label={t("formDialog.selectAllGroup", {
                                    module: group.label,
                                  })}
                                  onCheckedChange={(value) =>
                                    toggleGroup(group.codes, value === true)
                                  }
                                />
                                <AccordionTrigger className="flex-1">
                                  <span>{group.label}</span>
                                  <span className="ml-auto text-xs font-normal text-muted-foreground tabular-nums">
                                    {selectedInGroup}/{group.codes.length}
                                  </span>
                                </AccordionTrigger>
                              </div>

                              <AccordionContent className="space-y-4 pl-7">
                                {group.subgroups.map((subgroup) => (
                                  <div key={subgroup.label} className="space-y-2">
                                    {/* Con un solo subgrupo el encabezado repetiría
                                        el nombre del módulo, así que se omite. */}
                                    {group.subgroups.length > 1 && (
                                      <p className="text-xs font-semibold text-muted-foreground uppercase">
                                        {subgroup.label}
                                      </p>
                                    )}
                                    {subgroup.items.map((permission) => {
                                      // id/htmlFor explícitos en vez de <FormControl>:
                                      // hay un solo FormItem para los 29 permisos y
                                      // FormControl le pondría a todos el mismo id.
                                      const inputId = `permission-${permission.code}`
                                      return (
                                        <div
                                          key={permission.code}
                                          className="flex items-start gap-2"
                                        >
                                          <Checkbox
                                            id={inputId}
                                            className="mt-0.5"
                                            checked={selected.has(permission.code)}
                                            disabled={isSystemRole}
                                            onCheckedChange={(value) =>
                                              togglePermission(
                                                permission.code,
                                                value === true
                                              )
                                            }
                                          />
                                          <label
                                            htmlFor={inputId}
                                            className="text-sm leading-tight font-normal peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                                          >
                                            {permission.description}
                                          </label>
                                        </div>
                                      )
                                    })}
                                  </div>
                                ))}
                              </AccordionContent>
                            </AccordionItem>
                          )
                        })}
                      </Accordion>
                    </div>
                    <FormMessage />
                  </FormItem>
                )
              }}
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
