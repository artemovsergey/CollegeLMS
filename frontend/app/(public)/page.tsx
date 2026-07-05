"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import Image from "next/image"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"

export default function HomePage() {
  const [news, setNews] = useState<NewsResponse[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=6")
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setNews(body.data.items)
        }
      })
      .catch(() => setError("Не удалось загрузить новости"))
  }, [])

  return (
    <div>
      {/* Hero */}
      <section className="relative flex min-h-[90vh] items-center justify-center overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-primary via-primary/90 to-primary/70" />
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage:
              "radial-gradient(circle at 25% 50%, rgba(255,255,255,0.3) 0%, transparent 50%), radial-gradient(circle at 75% 30%, rgba(255,255,255,0.15) 0%, transparent 50%)",
          }}
        />
        <div className="relative z-10 mx-auto max-w-4xl px-4 text-center">
          <h1 className="mb-4 text-4xl font-bold leading-tight text-white md:text-5xl lg:text-6xl">
            ГБПОУ «Ставропольский колледж связи имени В.А. Петрова»
          </h1>
          <p className="mb-8 text-lg text-white/90 md:text-xl">
            Качество. Традиции. Будущее.
          </p>
          <div className="flex flex-col items-center gap-4 sm:flex-row sm:justify-center">
            <Link
              href="/admissions"
              className="inline-flex h-12 items-center justify-center rounded-xl bg-white px-8 text-base font-semibold text-primary shadow-sm transition-colors hover:bg-white/90"
            >
              Поступить
            </Link>
            <Link
              href="/education"
              className="inline-flex h-12 items-center justify-center rounded-xl border-2 border-white/80 px-8 text-base font-semibold text-white transition-colors hover:bg-white/10"
            >
              Специальности
            </Link>
          </div>
        </div>
      </section>

      {/* About */}
      <section className="py-16 sm:py-20 lg:py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="mb-4 text-2xl font-bold text-foreground sm:text-3xl">О колледже</h2>
            <p className="text-base leading-relaxed text-muted-foreground sm:text-lg">
              Государственное бюджетное профессиональное образовательное учреждение
              «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
              готовит специалистов в области связи, программирования,
              радиоэлектронной техники и информационных технологий.
              Обучение ведётся на базе 9 и 11 классов.
            </p>
          </div>
        </div>
      </section>

      {/* News */}
      <section className="bg-muted py-16 sm:py-20 lg:py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-10 flex items-center justify-between">
            <h2 className="text-2xl font-bold text-foreground sm:text-3xl">Последние новости</h2>
            <Button variant="ghost" asChild>
              <Link href="/news">Все новости →</Link>
            </Button>
          </div>
          {error && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {error}
            </div>
          )}
          {news.length === 0 && !error ? (
            <p className="text-center text-muted-foreground">Загрузка...</p>
          ) : (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {news.map((item) => (
                <Link
                  key={item.id}
                  href={`/news/${item.id}`}
                  className="group rounded-xl border border-border bg-card p-0 transition-all duration-200 hover:border-primary/30 hover:shadow-md overflow-hidden"
                >
                  {item.imageUrl && (
                    <div className="relative h-44 overflow-hidden">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={item.imageUrl}
                        alt=""
                        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                      />
                    </div>
                  )}
                  <div className={item.imageUrl ? "p-5" : "p-5"}>
                    <p className="mb-1.5 text-xs text-muted-foreground">
                      {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                      {item.categoryName && ` · ${item.categoryName}`}
                    </p>
                    <h3 className="text-sm font-semibold text-foreground line-clamp-2 group-hover:text-primary transition-colors">
                      {item.title}
                    </h3>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
