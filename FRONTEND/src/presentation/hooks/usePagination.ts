import { useEffect, useState } from "react"

interface UsePaginationResult<T> {
  page: number
  setPage: (page: number) => void
  pageCount: number
  paginated: T[]
  totalCount: number
}

/**
 * Pagina en el cliente un arreglo ya cargado en memoria (no dispara nuevos fetches).
 * Vuelve a la página 1 automáticamente cuando cambia la cantidad de elementos
 * (ej: al aplicar un filtro o al crear/eliminar un registro).
 */
export function usePagination<T>(
  items: T[] | undefined,
  pageSize = 10
): UsePaginationResult<T> {
  const [page, setPage] = useState(1)
  const safeItems = items ?? []

  useEffect(() => {
    setPage(1)
  }, [safeItems.length])

  const pageCount = Math.max(1, Math.ceil(safeItems.length / pageSize))
  const clampedPage = Math.min(page, pageCount)
  const paginated = safeItems.slice(
    (clampedPage - 1) * pageSize,
    clampedPage * pageSize
  )

  return { page: clampedPage, setPage, pageCount, paginated, totalCount: safeItems.length }
}
