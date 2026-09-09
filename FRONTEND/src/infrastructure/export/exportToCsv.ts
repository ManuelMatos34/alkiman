export interface CsvSection {
  title: string
  headers: string[]
  rows: (string | number | null | undefined)[][]
}

/**
 * Genera y descarga un archivo CSV con múltiples secciones separadas por líneas en blanco.
 * Incluye BOM UTF-8 para compatibilidad con Excel en Windows.
 */
export function exportSectionsToCsv(filename: string, sections: CsvSection[]): void {
  const lines: string[] = []

  for (const section of sections) {
    lines.push(escapeField(section.title))
    lines.push(section.headers.map(escapeField).join(","))

    for (const row of section.rows) {
      lines.push(row.map((v) => escapeField(v == null ? "" : String(v))).join(","))
    }

    lines.push("") // blank separator between sections
  }

  const blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8" })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = filename.endsWith(".csv") ? filename : `${filename}.csv`
  anchor.click()
  URL.revokeObjectURL(url)
}

function escapeField(value: string): string {
  if (value.includes(",") || value.includes('"') || value.includes("\n") || value.includes("\r")) {
    return '"' + value.replace(/"/g, '""') + '"'
  }
  return value
}
