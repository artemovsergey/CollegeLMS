"use client"

import { useEffect, useState, useCallback } from "react"
import Link from "next/link"
import type { Result, NewsResponse, NewsCategoryResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"

const ITEMS_PER_PAGE = 12

export default function NewsListPage() {
  const [news, setNews] = useState<NewsResponse[]>([])
  const [categories, setCategories] = useState<NewsCategoryResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>()
  const [search, setSearch] = useState("")
  const [searchInput, setSearchInput] = useState("")

  const fetchNews = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const params = new URLSearchParams()
      params.set("page", String(page))
      params.set("pageSize", String(ITEMS_PER_PAGE))
      if (categoryFilter) params.set("categoryId", categoryFilter)
      if (search) params.set("search", search)

      const res = await api.get<Result<PagedResponse<NewsResponse>>>(
        `/api/news?${params.toString()}`
      )
      const body = res.data
      if (body.isSuccess && body.data) {
        setNews(body.data.items)
        setTotalPages(body.data.totalPages)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки новостей")
    } finally {
      setLoading(false)
    }
  }, [page, categoryFilter, search])

  useEffect(() => {
    api
      .get<Result<NewsCategoryResponse[]>>("/api/news/categories")
      .then(res => {
        if (res.data.isSuccess && res.data.data) {
          setCategories(res.data.data)
        }
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    fetchNews()
  }, [fetchNews])

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    setSearch(searchInput)
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold text-[#152851]">Новости</h1>
        <p className="mt-1 text-sm text-[#5a6a8a]">
          Последние события и объявления колледжа
        </p>
      </div>

      {/* Filters */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => {
              setCategoryFilter(undefined)
              setPage(1)
            }}
            className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#568cd6] focus-visible:ring-offset-2 ${
              !categoryFilter
                ? "bg-[#568cd6] text-white"
                : "bg-[#e4edf8] text-[#152851] hover:bg-[#d4d9e3]"
            }`}
          >
            Все
          </button>
          {categories.map(cat => (
            <button
              key={cat.id}
              onClick={() => {
                setCategoryFilter(cat.id)
                setPage(1)
              }}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#568cd6] focus-visible:ring-offset-2 ${
                categoryFilter === cat.id
                  ? "bg-[#568cd6] text-white"
                  : "bg-[#e4edf8] text-[#152851] hover:bg-[#d4d9e3]"
              }`}
            >
              {cat.name}
            </button>
          ))}
        </div>
        <form onSubmit={handleSearch} className="flex gap-2">
          <input
            type="text"
            value={searchInput}
            onChange={e => setSearchInput(e.target.value)}
            placeholder="Поиск..."
            className="rounded-md border border-[#d4d9e3] px-3 py-1.5 text-sm outline-none focus:border-[#568cd6] focus:ring-2 focus:ring-[#568cd6]/30"
          />
          <Button type="submit" size="sm">
            Найти
          </Button>
        </form>
      </div>

      {/* Error */}
      {error && (
        <div className="mb-6 rounded-md bg-[#f8e8e8] p-3 text-sm text-[#c43e3e]">
          {error}
        </div>
      )}

      {/* Loading */}
      {loading ? (
        <div className="flex justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[#d4d9e3] border-t-[#568cd6]" />
        </div>
      ) : news.length === 0 ? (
        <div className="py-20 text-center">
          <p className="text-[#5a6a8a]">Новостей пока нет</p>
        </div>
      ) : (
        <>
          {/* Grid */}
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {news.map(item => (
              <Link
                key={item.id}
                href={`/news/${item.id}`}
                className="group rounded-lg border border-[#d4d9e3] bg-white p-5 transition-all duration-200 hover:border-[#568cd6]/30 hover:shadow-sm"
              >
                {item.imageUrl && (
                  <div className="mb-3 overflow-hidden rounded-md">
                    <img
                      src={item.imageUrl}
                      alt=""
                      className="h-40 w-full object-cover transition-transform duration-200 group-hover:scale-105"
                    />
                  </div>
                )}
                <p className="mb-1 text-xs text-[#5a6a8a]">
                  {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                  {item.categoryName && ` · ${item.categoryName}`}
                </p>
                <h3 className="text-sm font-semibold text-[#152851] line-clamp-2">{item.title}</h3>
              </Link>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-10 flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
              >
                ← Назад
              </Button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
                <button
                  key={p}
                  onClick={() => setPage(p)}
                  className={`flex h-8 w-8 items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#568cd6] focus-visible:ring-offset-2 ${
                    p === page
                      ? "bg-[#568cd6] text-white"
                      : "text-[#5a6a8a] hover:bg-[#e4edf8]"
                  }`}
                >
                  {p}
                </button>
              ))}
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              >
                Вперед →
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
