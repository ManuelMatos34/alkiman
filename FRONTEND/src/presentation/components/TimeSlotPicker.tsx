import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"
import { Loader2 } from "lucide-react"
import { Input } from "@/components/ui/input"
import { getIntlLocale } from "@/infrastructure/i18n/localeMap"
import { useBarbershopSlots, usePublicBarbershopSlots } from "@/application/barbershop/useBarbershopAppointments"
import { cn } from "@/lib/utils"

function localDateString(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, "0")
  const day = String(d.getDate()).padStart(2, "0")
  return `${d.getFullYear()}-${m}-${day}`
}

interface TimeSlotPickerBaseProps {
  value: string
  onChange: (iso: string) => void
  serviceId?: number | null
  disabled?: boolean
  defaultDate?: string
}

interface InternalProps extends TimeSlotPickerBaseProps {
  mode: "internal"
  slug?: never
  defaultDate?: string
}

interface PublicProps extends TimeSlotPickerBaseProps {
  mode: "public"
  slug: string
  defaultDate?: string
}

type TimeSlotPickerProps = InternalProps | PublicProps

// Inner component that uses the appropriate hook depending on mode
function InternalSlotGrid({
  date,
  serviceId,
  selectedIso,
  onSelect,
}: {
  date: string
  serviceId?: number | null
  selectedIso: string
  onSelect: (iso: string) => void
}) {
  const { data: slots, isLoading } = useBarbershopSlots(date, serviceId)
  return <SlotGrid slots={slots} isLoading={isLoading} selectedIso={selectedIso} onSelect={onSelect} />
}

function PublicSlotGrid({
  slug,
  date,
  serviceId,
  selectedIso,
  onSelect,
}: {
  slug: string
  date: string
  serviceId?: number | null
  selectedIso: string
  onSelect: (iso: string) => void
}) {
  const { data: slots, isLoading } = usePublicBarbershopSlots(slug, date, serviceId)
  return <SlotGrid slots={slots} isLoading={isLoading} selectedIso={selectedIso} onSelect={onSelect} />
}

function SlotGrid({
  slots,
  isLoading,
  selectedIso,
  onSelect,
}: {
  slots: { slotTime: string; isAvailable: boolean }[] | undefined
  isLoading: boolean
  selectedIso: string
  onSelect: (iso: string) => void
}) {
  const { t, i18n } = useTranslation("barbershop")

  const timeFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(getIntlLocale(i18n.language), {
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
      }),
    [i18n.language]
  )

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-6">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!slots || slots.length === 0) {
    return (
      <p className="py-4 text-center text-sm text-muted-foreground">
        {t("timeSlots.noSlots")}
      </p>
    )
  }

  return (
    <div className="grid grid-cols-4 gap-2 sm:grid-cols-5 md:grid-cols-6">
      {slots.map((slot) => {
        // Normalize both to compare: slotTime from backend may include seconds
        const slotIso = slot.slotTime.slice(0, 16) // "YYYY-MM-DDTHH:mm"
        const selectedNorm = selectedIso.slice(0, 16)
        const isSelected = slotIso === selectedNorm

        if (!slot.isAvailable) {
          return (
            <div
              key={slot.slotTime}
              className="flex items-center justify-center rounded-md border px-2 py-2 text-xs font-medium border-red-200 bg-red-50 text-red-400 cursor-not-allowed opacity-60 select-none"
            >
              {timeFormatter.format(new Date(slot.slotTime))}
            </div>
          )
        }

        return (
          <button
            key={slot.slotTime}
            type="button"
            onClick={() => onSelect(slotIso)}
            className={cn(
              "flex items-center justify-center rounded-md border px-2 py-2 text-xs font-medium transition-colors",
              isSelected
                ? "border-primary bg-primary text-primary-foreground"
                : "border-emerald-300 bg-emerald-100 text-emerald-800 hover:bg-emerald-200"
            )}
          >
            {timeFormatter.format(new Date(slot.slotTime))}
          </button>
        )
      })}
    </div>
  )
}

export function TimeSlotPicker({
  value,
  onChange,
  serviceId,
  disabled,
  defaultDate,
  mode,
  slug,
}: TimeSlotPickerProps) {
  const { t } = useTranslation("barbershop")

  // Use local date (not UTC) so it matches the board's date state
  const initialDate = value
    ? value.slice(0, 10)
    : defaultDate ?? localDateString(new Date())
  const [selectedDate, setSelectedDate] = useState<string>(initialDate)

  function handleDateChange(e: React.ChangeEvent<HTMLInputElement>) {
    const newDate = e.target.value
    setSelectedDate(newDate)
    // Clear the time selection when date changes
    onChange("")
  }

  function handleSlotSelect(iso: string) {
    // iso is "YYYY-MM-DDTHH:mm" — combine with current date
    onChange(iso)
  }

  return (
    <div className="space-y-3">
      <Input
        type="date"
        value={selectedDate}
        onChange={handleDateChange}
        disabled={disabled}
        min={new Date().toISOString().slice(0, 10)}
      />

      {selectedDate && (
        <div className="rounded-md border bg-muted/30 p-3">
          <p className="mb-2 text-xs font-medium text-muted-foreground">
            {t("timeSlots.selectTime")}
          </p>
          {mode === "internal" ? (
            <InternalSlotGrid
              date={selectedDate}
              serviceId={serviceId}
              selectedIso={value}
              onSelect={handleSlotSelect}
            />
          ) : (
            <PublicSlotGrid
              slug={slug!}
              date={selectedDate}
              serviceId={serviceId}
              selectedIso={value}
              onSelect={handleSlotSelect}
            />
          )}
        </div>
      )}
    </div>
  )
}
