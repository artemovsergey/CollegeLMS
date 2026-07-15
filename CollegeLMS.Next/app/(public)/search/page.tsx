"use client"

import { useState, useEffect, useCallback, useRef, Suspense, type FormEvent } from "react"
import { useSearchParams, useRouter } from "next/navigation"
import Link from "next/link"
import { Search as SearchIcon } from "lucide-react"
import type { Result, PagedResponse, SearchResult } from "@/types"
import api from "@/lib/api"

function SearchResults() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const query = searchParams.get("q") || ""

  const [inputValue, setInputValue] = useState(query)
  const [results, setResults] = useState<SearchResult[]>([])
  const [suggestions, setSuggestions] = useState<SearchResult[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [showSuggestions, setShowSuggestions] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(-1)
  const inputRef = useRef<HTMLInputElement>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout>>()
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
    setInputValue(query)
    if (query) fetchResults(query, 1)
  }, [query, fetchResults])

  const fetchSuggestions = useCallback(async (q: string) => {
    if (!q.trim()) {
      setSuggestions([])
      return
    }
    try {
      const res = await api.get<Result<PagedResponse<SearchResult>>>("/api/search", {
        params: { q, page: 1, pageSize: 5 },
      })
      const body = res.data
      if (body.isSuccess && body.data) {
        setSuggestions(body.data.items)
        setShowSuggestions(true)
      }
    } catch {
    }
  }, [])

  const handleInputChange = (value: string) => {
    setInputValue(value)
    setSelectedIndex(-1)

    if (debounceRef.current) clearTimeout(debounceRef.current)

    if (value.trim()) {
      debounceRef.current = setTimeout(() => {
        fetchSuggestions(value.trim())
      }, 300)
    } else {
      setSuggestions([])
      setShowSuggestions(false)
    }
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setShowSuggestions(false)
    const trimmed = inputValue.trim()
    if (trimmed) {
      router.push(`/search?q=${encodeURIComponent(trimmed)}`)
    }
  }

  function selectSuggestion(suggestion: SearchResult) {
    setShowSuggestions(false)
    setInputValue(suggestion.title)
    router.push(suggestion.url)
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (!showSuggestions || suggestions.length === 0) return

    if (e.key === "ArrowDown") {
      e.preventDefault()
      setSelectedIndex(i => Math.min(i + 1, suggestions.length - 1))
    } else if (e.key === "ArrowUp") {
      e.preventDefault()
      setSelectedIndex(i => Math.max(i - 1, 0))
    } else if (e.key === "Enter" && selectedIndex >= 0) {
      e.preventDefault()
      selectSuggestion(suggestions[selectedIndex])
    } else if (e.key === "Escape") {
      setShowSuggestions(false)
    }
  }

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <h1 className="mb-2 text-2xl font-bold text-primary">Поиск</h1>

      <form onSubmit={handleSubmit} className="relative mb-6">
        <input
          ref={inputRef}
          type="text"
          value={inputValue}
          onChange={e => handleInputChange(e.target.value)}
          onFocus={() => { if (suggestions.length > 0) setShowSuggestions(true) }}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
          onKeyDown={handleKeyDown}
          placeholder="Поиск по новостям и страницам..."
          className="w-full rounded-lg border border-border bg-card px-4 py-2.5 pr-12 text-sm text-primary outline-none transition-colors placeholder:text-muted-foreground focus:border-accent"
          autoFocus
        />
        <button
          type="submit"
          className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:text-accent"
          aria-label="Найти"
        >
          <SearchIcon className="h-5 w-5" />
        </button>

        {showSuggestions && suggestions.length > 0 && (
          <div className="absolute top-full left-0 right-0 z-10 mt-1 rounded-lg border border-border bg-card shadow-lg">
            {suggestions.map((item, i) => (
              <button
                key={`${item.type}-${item.url}-${i}`}
                type="button"
                onMouseDown={() => selectSuggestion(item)}
                className={`flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm transition-colors ${
                  i === selectedIndex ? "bg-accent/10" : "hover:bg-accent/5"
                } ${i === 0 ? "rounded-t-lg" : ""} ${i === suggestions.length - 1 ? "rounded-b-lg" : ""}`}
              >
                <span
                  className={`shrink-0 rounded px-1.5 py-0.5 text-xs font-medium ${
                    item.type === "news"
                      ? "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300"
                      : "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300"
                  }`}
                >
                  {item.type === "news" ? "Новость" : "Страница"}
                </span>
                <span className="truncate">{item.title}</span>
              </button>
            ))}
          </div>
        )}
      </form>

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
                  setPage(p => Math.max(1, p - 1))
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
                  setPage(p => Math.min(totalPages, p + 1))
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
