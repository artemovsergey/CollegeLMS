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
    <div className="app-section">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <div className="flex items-end justify-between mb-10">
          <div>
            <h2 className="app-section__title mb-0">Календарь событий</h2>
            <p className="app-section__subtitle mb-0">Важные даты и мероприятия</p>
          </div>
          <Link
            href="/events"
            className="hidden sm:inline-flex items-center gap-1 text-sm font-medium text-tpu-blue hover:text-tpu-blue-hover transition-colors"
          >
            Все события →
          </Link>
        </div>

        <div className="grid gap-4 md:grid-cols-3">
          {events.map((event) => (
            <div
              key={event.title}
              className="rounded-lg border border-tpu-border bg-white p-6 transition-all hover:shadow-md"
            >
              <div className="mb-3 inline-block rounded bg-tpu-blue-light px-3 py-1.5 text-xs font-semibold text-tpu-blue">
                {event.date}
              </div>
              <h3 className="mb-2 text-base font-semibold text-tpu-text">
                {event.title}
              </h3>
              <p className="mb-4 text-sm leading-relaxed text-tpu-text-muted">
                {event.description}
              </p>
              <div className="flex items-center gap-1.5 text-xs text-tpu-text-muted">
                <MapPin size={14} />
                {event.location}
              </div>
            </div>
          ))}
        </div>

        <div className="mt-6 text-center sm:hidden">
          <Link
            href="/events"
            className="inline-flex items-center gap-1 text-sm font-medium text-tpu-blue hover:text-tpu-blue-hover transition-colors"
          >
            Все события →
          </Link>
        </div>
      </div>
    </div>
  )
}
