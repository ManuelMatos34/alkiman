import { useTranslation } from "react-i18next"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Button } from "@/components/ui/button"

interface TablePaginationProps {
  page: number
  pageCount: number
  totalCount: number
  pageSize: number
  onPageChange: (page: number) => void
}

/** Controles de paginación (rango, anterior/siguiente, indicador de página) para listados paginados en el cliente. */
export function TablePagination({
  page,
  pageCount,
  totalCount,
  pageSize,
  onPageChange,
}: TablePaginationProps) {
  const { t } = useTranslation("common")

  if (totalCount === 0) return null

  const rangeStart = (page - 1) * pageSize + 1
  const rangeEnd = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-col gap-3 border-t border-border/60 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-sm text-muted-foreground">
        {t("pagination.range", { start: rangeStart, end: rangeEnd, total: totalCount })}
      </p>
      <div className="flex items-center gap-3">
        <span className="text-sm text-muted-foreground">
          {t("pagination.pageIndicator", { page, pageCount })}
        </span>
        <div className="flex gap-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            <ChevronLeft className="h-4 w-4" />
            {t("pagination.previous")}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= pageCount}
            onClick={() => onPageChange(page + 1)}
          >
            {t("pagination.next")}
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}
