"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"

export default function HomePage() {
  const [news, setNews] = useState<NewsResponse[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=6")
      .then(res => {
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
      <section className="bg-[#e4edf8] py-20">
        <div className="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
          <h1 className="mb-4 text-3xl font-bold leading-tight text-[#152851] sm:text-4xl lg:text-5xl">
            Ставропольский колледж связи
          </h1>
          <p className="mx-auto mb-8 max-w-2xl text-lg text-[#5a6a8a]">
            Государственное бюджетное профессиональное образовательное учреждение
            имени Героя Советского Союза В.А. Петрова
          </p>
          <div className="flex justify-center gap-4">
            <Button asChild>
              <Link href="/news">Новости</Link>
            </Button>
            <Button variant="outline" asChild>
              <a href="http://stvcc.ru" target="_blank" rel="noopener noreferrer">
                Старый сайт
              </a>
            </Button>
          </div>
        </div>
      </section>

      {/* About */}
      <section className="py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="mb-4 text-2xl font-semibold text-[#152851]">О колледже</h2>
            <p className="text-base leading-relaxed text-[#5a6a8a]">
              Колледж готовит специалистов в области связи, программирования,
              радиоэлектронной техники и информационных технологий. Обучение
              ведётся на базе 9 и 11 классов. Выпускники колледжа востребованы
              на рынке труда и успешно работают на предприятиях связи
              Ставропольского края и всей России.
            </p>
          </div>
        </div>
      </section>

      {/* News */}
      <section className="bg-[#f5f7fa] py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-8 flex items-center justify-between">
            <h2 className="text-2xl font-semibold text-[#152851]">Последние новости</h2>
            <Button variant="ghost" asChild>
              <Link href="/news">Все новости →</Link>
            </Button>
          </div>
          {error && (
            <div className="rounded-md bg-[#f8e8e8] p-3 text-sm text-[#c43e3e]">
              {error}
            </div>
          )}
          {news.length === 0 && !error ? (
            <p className="text-center text-[#5a6a8a]">Загрузка...</p>
          ) : (
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
                  <h3 className="text-sm font-semibold text-[#152851] line-clamp-2">
                    {item.title}
                  </h3>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Contacts */}
      <section className="py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <h2 className="mb-8 text-center text-2xl font-semibold text-[#152851]">Контакты</h2>
          <div className="mx-auto grid max-w-3xl gap-6 sm:grid-cols-3">
            <div className="rounded-lg border border-[#d4d9e3] bg-white p-5 text-center">
              <p className="mb-1 text-xs font-medium text-[#5a6a8a]">Адрес</p>
              <p className="text-sm font-medium text-[#152851]">
                г. Ставрополь, ул. Петрова, 123
              </p>
            </div>
            <div className="rounded-lg border border-[#d4d9e3] bg-white p-5 text-center">
              <p className="mb-1 text-xs font-medium text-[#5a6a8a]">Телефон</p>
              <p className="text-sm font-medium text-[#152851]">+7 (8652) 00-00-00</p>
            </div>
            <div className="rounded-lg border border-[#d4d9e3] bg-white p-5 text-center">
              <p className="mb-1 text-xs font-medium text-[#5a6a8a]">Email</p>
              <p className="text-sm font-medium text-[#152851]">info@stvcc.ru</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}
