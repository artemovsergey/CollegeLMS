"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"

export default function NewsSectionTPU() {
  const [news, setNews] = useState<NewsResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=6")
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setNews(body.data.items)
        }
        setLoading(false)
      })
      .catch(() => {
        setError("Не удалось загрузить новости")
        setLoading(false)
      })
  }, [])

  return (
    <section className="bg-[var(--color-tpu-bg-muted)] py-[var(--section-padding-y)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 flex items-end justify-between">
          <div>
            <h2 className="text-3xl font-bold text-[var(--color-tpu-text-primary)]">
              Последние новости
            </h2>
            <p className="text-[var(--color-tpu-text-secondary)] mt-2">
              События и достижения колледжа
            </p>
          </div>
          <Link
            href="/news"
            className="hidden sm:inline-flex items-center gap-1 text-sm font-medium text-[var(--color-tpu-accent)] hover:text-[var(--color-tpu-accent-hover)] transition-colors"
          >
            Все новости →
          </Link>
        </div>

        {error && (
          <div className="rounded-lg bg-red-50 p-4 text-sm text-red-600">{error}</div>
        )}

        {loading ? (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="animate-pulse rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] overflow-hidden">
                <div className="h-48 bg-gray-200" />
                <div className="p-5 space-y-3">
                  <div className="h-3 w-24 rounded bg-gray-200" />
                  <div className="h-4 w-3/4 rounded bg-gray-200" />
                  <div className="h-3 w-full rounded bg-gray-200" />
                </div>
              </div>
            ))}
          </div>
        ) : news.length === 0 && !error ? (
          <p className="text-center text-[var(--color-tpu-text-secondary)]">
            Новостей пока нет
          </p>
        ) : (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {news.slice(0, 3).map((item) => (
              <Link
                key={item.id}
                href={`/news/${item.id}`}
                className="group rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] overflow-hidden transition-all duration-200 hover:shadow-[var(--shadow-tpu-md)]"
              >
                {item.imageUrl && (
                  <div className="overflow-hidden">
                    <img
                      src={item.imageUrl}
                      alt=""
                      className="h-48 w-full object-cover transition-transform duration-300 group-hover:scale-105"
                    />
                  </div>
                )}
                <div className="p-5">
                  <p className="mb-2 text-xs text-[var(--color-tpu-text-secondary)]">
                    {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                    {item.categoryName && ` · ${item.categoryName}`}
                  </p>
                  <h3 className="mb-2 text-sm font-semibold text-[var(--color-tpu-text-primary)] line-clamp-2">
                    {item.title}
                  </h3>
                  {item.content && (
                    <p className="text-xs text-[var(--color-tpu-text-secondary)] line-clamp-2">
                      {item.content.replace(/<[^>]*>/g, "").slice(0, 120)}
                    </p>
                  )}
                </div>
              </Link>
            ))}
          </div>
        )}

        <div className="mt-8 text-center sm:hidden">
          <Link
            href="/news"
            className="inline-flex items-center gap-1 text-sm font-medium text-[var(--color-tpu-accent)] hover:text-[var(--color-tpu-accent-hover)] transition-colors"
          >
            Все новости →
          </Link>
        </div>
      </div>
    </section>
  )
}
