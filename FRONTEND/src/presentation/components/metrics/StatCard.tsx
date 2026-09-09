import type { LucideIcon } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"

export interface StatCardProps {
  label: string
  value: string
  hint?: string
  icon: LucideIcon
  tone?: "default" | "positive" | "negative"
}

export function StatCard({ label, value, hint, icon: Icon, tone = "default" }: StatCardProps) {
  return (
    <Card>
      <CardContent className="flex items-center justify-between gap-3 px-6">
        <div className="space-y-1">
          <p className="text-sm text-muted-foreground">{label}</p>
          <p
            className={
              "text-2xl font-semibold tracking-tight " +
              (tone === "positive"
                ? "text-emerald-600"
                : tone === "negative"
                  ? "text-destructive"
                  : "")
            }
          >
            {value}
          </p>
          {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
        </div>
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted">
          <Icon className="h-5 w-5 text-muted-foreground" />
        </div>
      </CardContent>
    </Card>
  )
}
