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
    <div className="app-section">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <h2 className="app-section__title">Новости</h2>
        <p className="app-section__subtitle">События и достижения колледжа</p>

        {error && (
          <div className="mb-6 rounded bg-red-50 p-4 text-sm text-red-600">{error}</div>
        )}

        {loading ? (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="animate-pulse rounded-lg border border-tpu-border bg-white overflow-hidden">
                <div className="h-48 bg-gray-200" />
                <div className="p-5 space-y-3">
                  <div className="h-3 w-24 rounded bg-gray-200" />
                  <div className="h-4 w-3/4 rounded bg-gray-200" />
                </div>
              </div>
            ))}
          </div>
        ) : news.length === 0 && !error ? (
          <p className="text-center text-tpu-text-muted">Новостей пока нет</p>
        ) : (
          <div className="news-grid-tpu">
            {news.slice(0, 3).map((item) => (
              <Link key={item.id} href={`/news/${item.id}`} className="news-card-tpu">
                {item.imageUrl && (
                  <img
                    src={item.imageUrl}
                    alt=""
                    className="news-card-tpu__image"
                  />
                )}
                <div className="news-card-tpu__body">
                  {item.categoryName && (
                    <span className="news-card-tpu__tag">{item.categoryName}</span>
                  )}
                  <div className="news-card-tpu__date">
                    {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                  </div>
                  <div className="news-card-tpu__title line-clamp-2">{item.title}</div>
                </div>
              </Link>
            ))}
          </div>
        )}

        <div className="mt-8 text-center">
          <Link
            href="/news"
            className="btn-tpu-accent"
          >
            Все новости
          </Link>
        </div>
      </div>
    </div>
  )
}
