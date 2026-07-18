"use client"

import { useEffect, useState } from "react"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import CarouselTPU from "./CarouselTPU"
import SpecialtiesSectionTPU from "./SpecialtiesSectionTPU"
import AdmissionSectionTPU from "./AdmissionSectionTPU"
import EventsSectionTPU from "./EventsSectionTPU"
import NewsSectionTPU from "./NewsSectionTPU"
import StatisticsSectionTPU from "./StatisticsSectionTPU"
import PartnersSectionTPU from "./PartnersSectionTPU"
import FAQSectionTPU from "./FAQSectionTPU"
import FeedbackFormTPU from "./FeedbackFormTPU"

export default function HomeTPU() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<PagedResponse<NewsResponse>>>("/api/news?page=1&pageSize=6")
      .then(() => setLoading(false))
      .catch(() => {
        setError("Не удалось загрузить данные")
        setLoading(false)
      })
  }, [])

  return (
    <div>
      <CarouselTPU />

      <section className="py-[var(--section-padding-y)]">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-4xl text-center">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full bg-[var(--color-tpu-accent-light)] px-4 py-1.5 text-xs font-semibold text-[var(--color-tpu-accent)]">
              Государственное бюджетное профессиональное образовательное учреждение
            </div>
            <h2 className="mb-6 text-3xl lg:text-4xl font-bold text-[var(--color-tpu-text-primary)] leading-tight">
              Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова
            </h2>
            <p className="text-base leading-relaxed text-[var(--color-tpu-text-secondary)] max-w-3xl mx-auto">
              Готовим востребованных специалистов в области связи, программирования,
              радиоэлектронной техники и информационных технологий.
              Обучение ведётся на базе 9 и 11 классов.
            </p>
          </div>
        </div>
      </section>

      <SpecialtiesSectionTPU />
      <AdmissionSectionTPU />
      <EventsSectionTPU />
      <NewsSectionTPU />
      <StatisticsSectionTPU />
      <PartnersSectionTPU />

      <section className="py-[var(--section-padding-y)]">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-12 text-center">
            <h2 className="mb-3 text-3xl font-bold text-[var(--color-tpu-text-primary)]">
              Обратная связь
            </h2>
            <p className="text-[var(--color-tpu-text-secondary)] max-w-2xl mx-auto">
              Есть вопрос или предложение? Напишите нам, и мы обязательно ответим.
            </p>
          </div>
          <FeedbackFormTPU />
        </div>
      </section>

      <FAQSectionTPU />
    </div>
  )
}
