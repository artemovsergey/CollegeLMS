"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import Carousel from "@/components/Carousel"
import StatisticsSection from "@/components/StatisticsSection"
import SpecialtiesSection from "@/components/SpecialtiesSection"
import AdmissionSection from "@/components/AdmissionSection"
import EventsSection from "@/components/EventsSection"
import PartnersSection from "@/components/PartnersSection"
import LicensesSection from "@/components/LicensesSection"
import MediaSection from "@/components/MediaSection"
import GraduateStoriesSection from "@/components/GraduateStoriesSection"
import FeedbackForm from "@/components/FeedbackForm"
import FAQSection from "@/components/FAQSection"

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
      <Carousel />

      <section className="py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="mb-4 text-2xl font-semibold text-primary">О колледже</h2>
            <p className="text-base leading-relaxed text-muted-foreground">
              Государственное бюджетное профессиональное образовательное учреждение
              «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
              готовит специалистов в области связи, программирования,
              радиоэлектронной техники и информационных технологий.
              Обучение ведётся на базе 9 и 11 классов.
            </p>
          </div>
        </div>
      </section>

      <StatisticsSection />
      <SpecialtiesSection />
      <AdmissionSection />
      <EventsSection />

      <section className="bg-muted py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-8 flex items-center justify-between">
            <h2 className="text-2xl font-semibold text-primary">Последние новости</h2>
            <Button variant="ghost" asChild>
              <Link href="/news">Все новости →</Link>
            </Button>
          </div>
          {error && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
          )}
          {news.length === 0 && !error ? (
            <p className="text-center text-muted-foreground">Загрузка...</p>
          ) : (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {news.map((item) => (
                <Link
                  key={item.id}
                  href={`/news/${item.id}`}
                  className="group rounded-lg border border-border bg-card p-5 transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
                >
                  {item.imageUrl && (
                    <div className="mb-3 overflow-hidden rounded-md">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={item.imageUrl}
                        alt=""
                        className="h-40 w-full object-cover transition-transform duration-200 group-hover:scale-105"
                      />
                    </div>
                  )}
                  <p className="mb-1 text-xs text-muted-foreground">
                    {new Date(item.publishedAt).toLocaleDateString("ru-RU")}
                    {item.categoryName && ` · ${item.categoryName}`}
                  </p>
                  <h3 className="text-sm font-semibold text-primary line-clamp-2">
                    {item.title}
                  </h3>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      <PartnersSection />
      <LicensesSection />
      <MediaSection />
      <GraduateStoriesSection />

      <section className="bg-muted py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Обратная связь</h2>
          <p className="mb-8 text-center text-sm text-muted-foreground">
            Есть вопрос или предложение? Напишите нам, и мы обязательно ответим.
          </p>
          <FeedbackForm />
        </div>
      </section>

      <FAQSection />
    </div>
  )
}
