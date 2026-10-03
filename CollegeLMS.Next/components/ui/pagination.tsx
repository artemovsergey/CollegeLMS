"use client"

import { Button } from "@/components/ui/button"

interface PaginationProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

export default function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  const getPages = () => {
    const pages: (number | "ellipsis")[] = []
    const delta = 1

    const rangeStart = Math.max(2, page - delta)
    const rangeEnd = Math.min(totalPages - 1, page + delta)

    pages.push(1)

    if (rangeStart > 2) {
      pages.push("ellipsis")
    }

    for (let i = rangeStart; i <= rangeEnd; i++) {
      pages.push(i)
    }

    if (rangeEnd < totalPages - 1) {
      pages.push("ellipsis")
    }

    if (totalPages > 1) {
      pages.push(totalPages)
    }

    return pages
  }

  const pages = getPages()

  return (
    /* Ряд номеров не помещался на 320px и давал прокрутку по горизонтали,
       поэтому переносится. Кнопки страниц — примитив Button: свой у примитива
       был отдельный рецепт кольца фокуса и размер 32px, ниже порога §6.1. */
    <nav
      aria-label="Постраничная навигация"
      className="mt-10 flex flex-wrap items-center justify-center gap-2"
    >
      <Button
        variant="outline"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        ← Назад
      </Button>
      {pages.map((p, i) =>
        p === "ellipsis" ? (
          <span key={`e-${i}`} className="px-1 text-muted-foreground">
            ...
          </span>
        ) : (
          <Button
            key={p}
            variant={p === page ? "default" : "ghost"}
            size="icon"
            aria-current={p === page ? "page" : undefined}
            onClick={() => onPageChange(p)}
          >
            {p}
          </Button>
        )
      )}
      <Button
        variant="outline"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Вперёд →
      </Button>
    </nav>
  )
}
