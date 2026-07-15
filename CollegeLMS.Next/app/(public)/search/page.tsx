"use client"

import { useState, useEffect, useCallback, Suspense } from "react"
import { useSearchParams } from "next/navigation"
import Link from "next/link"
import { Search as SearchIcon } from "lucide-react"
import type { Result, PagedResponse, SearchResult } from "@/types"
import api from "@/lib/api"

function SearchResults() {
  const searchParams = useSearchParams()
  const query = searchParams.get("q") || ""

  const [results, setResults] = useState<SearchResult[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const pageSize = 20

  const fetchResults = useCallback(
    async (q: string, p: number) => {
      if (!q.trim()) {
        setResults([])
        setTotalCount(0)
        return
      }

      setLoading(true)
      setError(null)

      try {
        const res = await api.get<Result<PagedResponse<SearchResult>>>("/api/search", {
          params: { q, page: p, pageSize },
        })
        const body = res.data
        if (body.isSuccess && body.data) {
          setResults(body.data.items)
          setTotalCount(body.data.totalCount)
        } else {
          setError(body.errorMessage || "Ошибка поиска")
        }
      } catch {
        setError("Не удалось выполнить поиск")
      } finally {
        setLoading(false)
      }
    },
    [pageSize]
  )

  useEffect(() => {
    setPage(1)
    fetchResults(query, 1)
  }, [query, fetchResults])

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <h1 className="mb-2 text-2xl font-bold text-primary">Поиск</h1>
      {query && (
        <p className="mb-6 text-sm text-muted-foreground">
          Результаты по запросу: &laquo;{query}&raquo;
          {!loading && ` (${totalCount})`}
        </p>
      )}

      {loading && (
        <div className="flex items-center justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
        </div>
      )}

      {error && (
        <div className="rounded-md bg-destructive/10 p-4 text-sm text-destructive">
          {error}
        </div>
      )}

      {!loading && !error && query && results.length === 0 && (
        <div className="rounded-lg border border-border bg-card p-8 text-center">
          <SearchIcon className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
          <p className="text-muted-foreground">Ничего не найдено</p>
        </div>
      )}

      {!loading && !error && results.length > 0 && (
        <>
          <div className="flex flex-col gap-3">
            {results.map((item, i) => (
              <Link
                key={`${item.type}-${item.url}-${i}`}
                href={item.url}
                className="rounded-lg border border-border bg-card p-5 transition-all hover:border-accent/30 hover:shadow-sm"
              >
                <div className="mb-1 flex items-center gap-2">
                  <span
                    className={`rounded px-2 py-0.5 text-xs font-medium ${
                      item.type === "news"
                        ? "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300"
                        : "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300"
                    }`}
                  >
                    {item.type === "news" ? "Новость" : "Страница"}
                  </span>
                  <h3 className="text-sm font-semibold text-primary">{item.title}</h3>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-2">{item.snippet}</p>
              </Link>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="mt-6 flex items-center justify-center gap-2">
              <button
                onClick={() => {
                  setPage((p) => Math.max(1, p - 1))
                  fetchResults(query, Math.max(1, page - 1))
                }}
                disabled={page === 1}
                className="rounded border border-border px-3 py-1.5 text-sm disabled:opacity-50"
              >
                Назад
              </button>
              <span className="text-sm text-muted-foreground">
                {page} / {totalPages}
              </span>
              <button
                onClick={() => {
                  setPage((p) => Math.min(totalPages, p + 1))
                  fetchResults(query, Math.min(totalPages, page + 1))
                }}
                disabled={page === totalPages}
                className="rounded border border-border px-3 py-1.5 text-sm disabled:opacity-50"
              >
                Далее
              </button>
            </div>
          )}
        </>
      )}

      {!query && (
        <div className="rounded-lg border border-border bg-card p-8 text-center">
          <SearchIcon className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
          <p className="text-muted-foreground">
            Введите поисковый запрос для поиска по новостям и страницам сайта
          </p>
        </div>
      )}
    </div>
  )
}

export default function SearchPage() {
  return (
    <Suspense
      fallback={
        <div className="flex items-center justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
        </div>
      }
    >
      <SearchResults />
    </Suspense>
  )
}
