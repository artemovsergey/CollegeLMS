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
import { useDesign } from "@/lib/design-provider"
import HomeTPU from "@/components/HomeTPU"

export default function HomePage() {
  const { design } = useDesign()
  if (design === "tpu") return <HomeTPU />

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
          {loading ? (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="animate-pulse rounded-lg border border-border bg-card p-5">
                  <div className="mb-3 h-40 rounded-md bg-muted" />
                  <div className="mb-2 h-3 w-24 rounded bg-muted" />
                  <div className="h-4 w-3/4 rounded bg-muted" />
                </div>
              ))}
            </div>
          ) : news.length === 0 && !error ? (
            <p className="text-center text-muted-foreground">Новостей пока нет</p>
          ) : (
            <div className="grid gap-0 border border-border rounded-lg overflow-hidden sm:grid-cols-2 lg:grid-cols-3">
              {news.map((item) => (
                <Link
                  key={item.id}
                  href={`/news/${item.id}`}
                  className="block bg-card p-5 transition-colors hover:bg-muted border-b border-r border-border"
                >
                  {item.imageUrl && (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={item.imageUrl} alt="" className="w-full h-40 object-cover mb-3 rounded-md" />
                  )}
                  {item.categoryName && (
                    <span className="inline-block text-xs font-medium text-white bg-accent px-2 py-0.5 rounded-sm mb-2">{item.categoryName}</span>
                  )}
                  <p className="text-xs text-muted-fg mb-1">{new Date(item.publishedAt).toLocaleDateString("ru-RU")}</p>
                  <h3 className="text-sm font-semibold text-fg line-clamp-2">{item.title}</h3>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>

      <StatisticsSection />
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
