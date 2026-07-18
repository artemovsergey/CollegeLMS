"use client"

import Link from "next/link"
import { MapPin } from "lucide-react"

const events = [
  {
    date: "15 августа 2026",
    title: "Окончание приёма документов (очная форма)",
    description: "Последний день подачи документов на очную форму обучения",
    location: "пер. Черняховского, 3",
  },
  {
    date: "20 августа 2026",
    title: "Зачисление на бюджетные места",
    description: "Издание приказа о зачислении абитуриентов на бюджетные места",
    location: "Колледж связи",
  },
  {
    date: "1 сентября 2026",
    title: "День знаний — Торжественная линейка",
    description: "Торжественное мероприятие, посвящённое началу учебного года",
    location: "пер. Черняховского, 3",
  },
]

export default function EventsSectionTPU() {
  return (
    <section className="py-[var(--section-padding-y)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 flex items-end justify-between">
          <div>
            <h2 className="text-3xl font-bold text-[var(--color-tpu-text-primary)]">
              Календарь событий
            </h2>
            <p className="text-[var(--color-tpu-text-secondary)] mt-2">
              Важные даты и мероприятия
            </p>
          </div>
          <Link
            href="/events"
            className="hidden sm:inline-flex items-center gap-1 text-sm font-medium text-[var(--color-tpu-accent)] hover:text-[var(--color-tpu-accent-hover)] transition-colors"
          >
            Все события →
          </Link>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {events.map((event) => (
            <div
              key={event.title}
              className="rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] p-6 transition-all duration-200 hover:shadow-[var(--shadow-tpu-md)]"
            >
              <div className="mb-3 inline-block rounded-lg bg-[var(--color-tpu-accent-light)] px-3 py-1.5 text-xs font-semibold text-[var(--color-tpu-accent)]">
                {event.date}
              </div>
              <h3 className="mb-2 text-base font-semibold text-[var(--color-tpu-text-primary)]">
                {event.title}
              </h3>
              <p className="mb-4 text-sm leading-relaxed text-[var(--color-tpu-text-secondary)]">
                {event.description}
              </p>
              <div className="flex items-center gap-1.5 text-xs text-[var(--color-tpu-text-secondary)]">
                <MapPin size={14} />
                {event.location}
              </div>
            </div>
          ))}
        </div>
        <div className="mt-6 text-center sm:hidden">
          <Link
            href="/events"
            className="inline-flex items-center gap-1 text-sm font-medium text-[var(--color-tpu-accent)] hover:text-[var(--color-tpu-accent-hover)] transition-colors"
          >
            Все события →
          </Link>
        </div>
      </div>
    </section>
  )
}
