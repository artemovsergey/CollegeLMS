"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import Link from "next/link"
import type { Result, NewsResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"

export default function NewsDetailPage() {
  const params = useParams()
  const router = useRouter()
  const [news, setNews] = useState<NewsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!params.id) return
    setLoading(true)
    setError(null)
    api
      .get<Result<NewsResponse>>(`/api/news/${params.id}`)
      .then(res => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setNews(body.data)
        } else {
          if (body.statusCode === 404) {
            setError("Новость не найдена")
          } else {
            setError(body.errorMessage ?? "Ошибка загрузки")
          }
        }
      })
      .catch(() => setError("Ошибка загрузки новости"))
      .finally(() => setLoading(false))
  }, [params.id])

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-border border-t-primary" />
      </div>
    )
  }

  if (error || !news) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-20 text-center">
        <p className="mb-4 text-lg text-destructive">{error ?? "Новость не найдена"}</p>
        <div className="flex justify-center gap-3">
          <Button variant="outline" onClick={() => router.back()}>
            ← Назад
          </Button>
          <Button asChild>
            <Link href="/news">Все новости</Link>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <article className="mx-auto max-w-3xl px-4 py-10 sm:px-6">
      <Button variant="ghost" size="sm" className="mb-6" asChild>
        <Link href="/news">← Все новости</Link>
      </Button>

      {news.imageUrl && (
        <div className="mb-6 overflow-hidden rounded-lg">
          <img
            src={news.imageUrl}
            alt=""
            className="w-full object-cover"
          />
        </div>
      )}

      <p className="mb-2 text-sm text-muted-foreground">
        {new Date(news.publishedAt).toLocaleDateString("ru-RU", {
          year: "numeric",
          month: "long",
          day: "numeric",
        })}
        {news.categoryName && ` · ${news.categoryName}`}
      </p>

      <h1 className="mb-6 text-2xl font-bold leading-tight text-foreground sm:text-3xl">
        {news.title}
      </h1>

      <div
        className="prose prose-sm max-w-none text-muted-foreground"
        dangerouslySetInnerHTML={{ __html: news.content }}
      />

      <div className="mt-10 border-t border-border pt-4 text-xs text-muted-foreground">
        Опубликовано: {news.createdByName}
      </div>
    </article>
  )
}
