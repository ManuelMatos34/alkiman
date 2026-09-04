import { useState } from "react"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { Plus, Pencil, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { CustomerFormDialog } from "@/presentation/components/CustomerFormDialog"
import { DeleteConfirmDialog } from "@/presentation/components/DeleteConfirmDialog"
import { TablePagination } from "@/presentation/components/TablePagination"
import { usePagination } from "@/presentation/hooks/usePagination"
import { useCustomers } from "@/application/customers/useCustomers"
import { useDeleteCustomer } from "@/application/customers/useDeleteCustomer"
import type { Customer } from "@/domain/types/customer"

export function CustomersPage() {
  const { t } = useTranslation("customers")
  const { data: customers, isLoading } = useCustomers()
  const deleteCustomer = useDeleteCustomer()
  const { page, setPage, pageCount, paginated: paginatedCustomers, totalCount } = usePagination(customers, 10)

  const [formOpen, setFormOpen] = useState(false)
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(
    null
  )
  const [deleteTarget, setDeleteTarget] = useState<Customer | null>(null)

  function handleCreate() {
    setEditingCustomer(null)
    setFormOpen(true)
  }

  function handleEdit(customer: Customer) {
    setEditingCustomer(customer)
    setFormOpen(true)
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return

    deleteCustomer.mutate(deleteTarget.id, {
      onSuccess: () => toast.success(t("toast.deleted")),
      onError: () => toast.error(t("toast.deleteError")),
    })
    setDeleteTarget(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4" />
          {t("newCustomer")}
        </Button>
      </div>

      <div className="rounded-lg border border-border/60">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.headers.name")}</TableHead>
              <TableHead>{t("table.headers.identity")}</TableHead>
              <TableHead>{t("table.headers.phone")}</TableHead>
              <TableHead>{t("table.headers.email")}</TableHead>
              <TableHead className="w-[100px] text-right">
                {t("table.headers.actions")}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 4 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={5}>
                    <Skeleton className="h-6 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {!isLoading && customers?.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="h-24 text-center text-muted-foreground"
                >
                  {t("table.empty")}
                </TableCell>
              </TableRow>
            )}

            {paginatedCustomers.map((customer) => (
              <TableRow key={customer.id}>
                <TableCell className="font-medium">
                  {customer.fullName}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {customer.identityNumber ?? t("table.emptyValue")}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {customer.phone ?? t("table.emptyValue")}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {customer.email ?? t("table.emptyValue")}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => handleEdit(customer)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => setDeleteTarget(customer)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <TablePagination
          page={page}
          pageCount={pageCount}
          totalCount={totalCount}
          pageSize={10}
          onPageChange={setPage}
        />
      </div>

      <CustomerFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        customer={editingCustomer}
      />

      <DeleteConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title={t("delete.title")}
        description={t("delete.description", { name: deleteTarget?.fullName })}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
