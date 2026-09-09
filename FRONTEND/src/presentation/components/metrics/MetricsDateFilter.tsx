import { Download, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

const DEFAULT_PRESET_DAYS = [7, 30, 90] as const

/** Fecha local en formato YYYY-MM-DD sin convertir a UTC. */
export function toDateInputValue(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${date.getFullYear()}-${month}-${day}`
}

export function daysAgo(days: number): Date {
  const date = new Date()
  date.setDate(date.getDate() - days)
  return date
}

export interface MetricsDateFilterProps {
  from: string
  to: string
  onFromChange: (v: string) => void
  onToChange: (v: string) => void
  /** Label for the from input. */
  fromLabel: string
  /** Label for the to input. */
  toLabel: string
  /** Label for preset buttons, e.g. "Last {{count}} days". Receives {count}. */
  presetLabel: (count: number) => string
  /** Days for the preset buttons. Defaults to [7, 30, 90]. */
  presetDays?: readonly number[]
  /** When provided, shows an export button. */
  onExport?: () => void
  /** Label for the export button. */
  exportLabel?: string
  /** Shows a spinner on the export button while true. */
  isExporting?: boolean
}

export function MetricsDateFilter({
  from,
  to,
  onFromChange,
  onToChange,
  fromLabel,
  toLabel,
  presetLabel,
  presetDays = DEFAULT_PRESET_DAYS,
  onExport,
  exportLabel = "Exportar CSV",
  isExporting = false,
}: MetricsDateFilterProps) {
  function applyPreset(days: number) {
    onFromChange(toDateInputValue(daysAgo(days - 1)))
    onToChange(toDateInputValue(new Date()))
  }

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="space-y-1.5">
        <Label htmlFor="mf-from">{fromLabel}</Label>
        <Input
          id="mf-from"
          type="date"
          value={from}
          max={to}
          onChange={(e) => onFromChange(e.target.value)}
          className="w-40"
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="mf-to">{toLabel}</Label>
        <Input
          id="mf-to"
          type="date"
          value={to}
          min={from}
          onChange={(e) => onToChange(e.target.value)}
          className="w-40"
        />
      </div>

      <div className="flex flex-wrap gap-2">
        {presetDays.map((days) => (
          <Button key={days} variant="outline" size="sm" onClick={() => applyPreset(days)}>
            {presetLabel(days)}
          </Button>
        ))}
      </div>

      {onExport && (
        <Button
          variant="outline"
          size="sm"
          onClick={onExport}
          disabled={isExporting}
          className="ml-auto"
        >
          {isExporting ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Download className="mr-2 h-4 w-4" />
          )}
          {exportLabel}
        </Button>
      )}
    </div>
  )
}
