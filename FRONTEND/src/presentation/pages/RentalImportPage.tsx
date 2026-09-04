import { useRef, useState } from "react"
import { Link } from "react-router-dom"
import Papa from "papaparse"
import { toast } from "sonner"
import { useTranslation } from "react-i18next"
import { ArrowLeft, CheckCircle2, Download, FileUp, Upload, XCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { useImportRentals } from "@/application/rentalImports/useImportRentals"
import { rentalImportCsvHeaders } from "@/domain/types/rentalImport"
import type { ImportRentalsResponse, RentalImportRow } from "@/domain/types/rentalImport"

const TEMPLATE_EXAMPLE_ROW = [
  "Juan Pérez",
  "001-1234567-8",
  "8091234567",
  "juan@correo.com",
  "Apartamento 2B",
  "Inmuebles",
  "25000",
  "1",
  "Monthly",
  "2024-01-01",
  "2025-01-01",
  "300000",
  "Active",
]

function downloadCsvTemplate() {
  const csv = [rentalImportCsvHeaders.join(","), TEMPLATE_EXAMPLE_ROW.join(",")].join("\n")
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" })
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = "plantilla-importacion-alquileres.csv"
  link.click()
  URL.revokeObjectURL(url)
}

function mapCsvRow(raw: Record<string, string>): RentalImportRow {
  const clean = (value?: string) => (value?.trim() ? value.trim() : null)
  return {
    customerFullName: raw.customerFullName?.trim() ?? "",
    customerIdentityNumber: raw.customerIdentityNumber?.trim() ?? "",
    customerPhone: clean(raw.customerPhone),
    customerEmail: clean(raw.customerEmail),
    assetName: raw.assetName?.trim() ?? "",
    assetCategoryName: clean(raw.assetCategoryName),
    assetBasePrice: clean(raw.assetBasePrice),
    assetStock: clean(raw.assetStock),
    assetRentalType: clean(raw.assetRentalType),
    startDate: raw.startDate?.trim() ?? "",
    endDate: raw.endDate?.trim() ?? "",
    totalPrice: raw.totalPrice?.trim() ?? "",
    status: clean(raw.status),
  }
}

/**
 * Mantenimiento de migración: permite cargar en un solo lote alquileres que ya se llevaban
 * manualmente o en otro sistema. Cliente y activo se reutilizan si ya existen (por identificación
 * y nombre respectivamente) o se crean automáticamente con los datos de la fila.
 */
export function RentalImportPage() {
  const { t } = useTranslation("rentalImport")
  const [rows, setRows] = useState<RentalImportRow[]>([])
  const [fileName, setFileName] = useState<string | null>(null)
  const [result, setResult] = useState<ImportRentalsResponse | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const importRentals = useImportRentals()

  function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return

    setFileName(file.name)
    setResult(null)

    Papa.parse<Record<string, string>>(file, {
      header: true,
      skipEmptyLines: true,
      complete: (parsed) => {
        if (parsed.errors.length > 0) {
          toast.error(t("toast.parseError"))
          return
        }
        const parsedRows = parsed.data.map(mapCsvRow)
        if (parsedRows.length === 0) {
          toast.error(t("toast.noRows"))
          return
        }
        setRows(parsedRows)
        toast.success(t("toast.rowsLoaded", { count: parsedRows.length }))
      },
    })
  }

  function handleImport() {
    if (rows.length === 0) return

    importRentals.mutate(
      { rows },
      {
        onSuccess: (response) => {
          setResult(response)
          if (response.failed === 0) {
            toast.success(t("toast.importSuccess", { count: response.succeeded }))
          } else {
            toast.error(
              t("toast.importPartial", {
                succeeded: response.succeeded,
                failed: response.failed,
              })
            )
          }
        },
        onError: () => toast.error(t("toast.importError")),
      }
    )
  }

  function handleReset() {
    setRows([])
    setFileName(null)
    setResult(null)
    if (fileInputRef.current) fileInputRef.current.value = ""
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
        <Button variant="outline" asChild>
          <Link to="/rentas">
            <ArrowLeft className="h-4 w-4" />
            {t("backToRentals")}
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t("step1.title")}</CardTitle>
          <CardDescription>{t("step1.description")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
            <li>
              <strong className="text-foreground">{t("step1.requiredLabel")}</strong>{" "}
              {t("step1.requiredText")}
            </li>
            <li>
              <strong className="text-foreground">{t("step1.newAssetLabel")}</strong>{" "}
              {t("step1.newAssetText")}
            </li>
            <li>
              <strong className="text-foreground">{t("step1.statusLabel")}</strong>{" "}
              {t("step1.statusText")}
            </li>
            <li>{t("step1.dateFormat")}</li>
          </ul>
          <Button variant="outline" onClick={downloadCsvTemplate}>
            <Download className="h-4 w-4" />
            {t("step1.downloadTemplate")}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t("step2.title")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <Input
              ref={fileInputRef}
              type="file"
              accept=".csv"
              onChange={handleFileChange}
              className="sm:max-w-xs"
            />
            {fileName && <span className="text-sm text-muted-foreground">{fileName}</span>}
          </div>

          {rows.length > 0 && (
            <>
              <div className="max-h-80 overflow-auto rounded-lg border border-border/60">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t("step2.table.headers.row")}</TableHead>
                      <TableHead>{t("step2.table.headers.customer")}</TableHead>
                      <TableHead>{t("step2.table.headers.asset")}</TableHead>
                      <TableHead>{t("step2.table.headers.category")}</TableHead>
                      <TableHead>{t("step2.table.headers.startDate")}</TableHead>
                      <TableHead>{t("step2.table.headers.endDate")}</TableHead>
                      <TableHead>{t("step2.table.headers.totalPrice")}</TableHead>
                      <TableHead>{t("step2.table.headers.status")}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rows.map((row, index) => (
                      <TableRow key={index}>
                        <TableCell className="text-muted-foreground">{index + 1}</TableCell>
                        <TableCell className="font-medium">
                          {row.customerFullName || t("step2.table.emptyValue")}
                        </TableCell>
                        <TableCell>{row.assetName || t("step2.table.emptyValue")}</TableCell>
                        <TableCell className="text-muted-foreground">
                          {row.assetCategoryName || t("step2.table.emptyValue")}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {row.startDate || t("step2.table.emptyValue")}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {row.endDate || t("step2.table.emptyValue")}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {row.totalPrice || t("step2.table.emptyValue")}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {row.status || "Active"}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm text-muted-foreground">
                  {t("step2.rowsReady", { count: rows.length })}
                </p>
                <div className="flex gap-2">
                  <Button variant="outline" onClick={handleReset}>
                    {t("step2.clear")}
                  </Button>
                  <Button onClick={handleImport} disabled={importRentals.isPending}>
                    <Upload className="h-4 w-4" />
                    {importRentals.isPending
                      ? t("step2.importing")
                      : t("step2.importButton", { count: rows.length })}
                  </Button>
                </div>
              </div>
            </>
          )}

          {rows.length === 0 && !fileName && (
            <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-border/60 p-8 text-center text-sm text-muted-foreground">
              <FileUp className="h-8 w-8" />
              {t("step2.dropzone")}
            </div>
          )}
        </CardContent>
      </Card>

      {result && (
        <Card>
          <CardHeader>
            <CardTitle>{t("step3.title")}</CardTitle>
            <CardDescription>
              <span className="mt-1 flex flex-wrap gap-2">
                <Badge variant="secondary">{t("step3.totalRows", { count: result.total })}</Badge>
                <Badge variant="default">
                  {t("step3.succeeded", { count: result.succeeded })}
                </Badge>
                {result.failed > 0 && (
                  <Badge variant="destructive">
                    {t("step3.failed", { count: result.failed })}
                  </Badge>
                )}
              </span>
            </CardDescription>
          </CardHeader>
          <CardContent className="px-6">
            <div className="rounded-lg border border-border/60">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t("step3.table.headers.row")}</TableHead>
                    <TableHead>{t("step3.table.headers.result")}</TableHead>
                    <TableHead>{t("step3.table.headers.detail")}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.results.map((row) => (
                    <TableRow key={row.rowNumber}>
                      <TableCell className="text-muted-foreground">{row.rowNumber}</TableCell>
                      <TableCell>
                        {row.success ? (
                          <span className="flex items-center gap-1.5 text-sm font-medium text-emerald-600">
                            <CheckCircle2 className="h-4 w-4" /> {t("step3.imported")}
                          </span>
                        ) : (
                          <span className="flex items-center gap-1.5 text-sm font-medium text-destructive">
                            <XCircle className="h-4 w-4" /> {t("step3.failedLabel")}
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {row.success ? (
                          <span className="flex flex-wrap gap-1.5">
                            {row.customerCreated && (
                              <Badge variant="outline">{t("step3.newCustomer")}</Badge>
                            )}
                            {row.categoryCreated && (
                              <Badge variant="outline">{t("step3.newCategory")}</Badge>
                            )}
                            {row.assetCreated && (
                              <Badge variant="outline">{t("step3.newAsset")}</Badge>
                            )}
                            {!row.customerCreated && !row.categoryCreated && !row.assetCreated && (
                              <span>{t("step3.noneValue")}</span>
                            )}
                          </span>
                        ) : (
                          row.errorMessage
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
