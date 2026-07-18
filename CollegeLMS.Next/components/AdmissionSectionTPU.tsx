"use client"

import Link from "next/link"
import { CalendarDays, MapPin, Phone, Building, ShieldCheck, Bed } from "lucide-react"

const items = [
  {
    icon: CalendarDays,
    title: "Приём документов",
    description: "Очная форма: 19 июня — 15 августа 2026\nЗаочная форма: 19 июня — 1 декабря 2026",
  },
  {
    icon: MapPin,
    title: "Адрес",
    description: "355031, г. Ставрополь, пер. Черняховского, 3",
  },
  {
    icon: Phone,
    title: "Приёмная комиссия",
    description: "24-20-32\nОтветственный секретарь: +7 (918) 744-74-70",
  },
  {
    icon: Building,
    title: "Заочное отделение",
    description: "24-23-33",
  },
  {
    icon: ShieldCheck,
    title: "Отсрочка от армии",
    description: "Всем зачисленным на очную форму обучения предоставляется отсрочка до получения диплома",
  },
  {
    icon: Bed,
    title: "Общежитие",
    description: "Поступившим при необходимости предоставляется общежитие",
  },
]

export default function AdmissionSectionTPU() {
  return (
    <section className="bg-[var(--color-tpu-bg-muted)] py-[var(--section-padding-y)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 text-center">
          <h2 className="mb-3 text-3xl font-bold text-[var(--color-tpu-text-primary)]">
            Приёмная кампания 2026
          </h2>
          <p className="text-[var(--color-tpu-text-secondary)] max-w-2xl mx-auto">
            Информация для абитуриентов и их родителей
          </p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div
              key={item.title}
              className="flex gap-4 rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] p-5 hover:shadow-[var(--shadow-tpu-sm)] transition-shadow"
            >
              <span className="mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[var(--color-tpu-accent-light)] text-[var(--color-tpu-accent)]">
                <item.icon size={20} />
              </span>
              <div>
                <h3 className="mb-1 text-sm font-semibold text-[var(--color-tpu-text-primary)]">
                  {item.title}
                </h3>
                <p className="whitespace-pre-line text-xs leading-relaxed text-[var(--color-tpu-text-secondary)]">
                  {item.description}
                </p>
              </div>
            </div>
          ))}
        </div>

        <div className="mt-10 text-center">
          <Link
            href="/admissions"
            className="inline-flex items-center gap-2 px-6 py-3 bg-[var(--color-tpu-accent)] text-white text-sm font-medium rounded-lg hover:bg-[var(--color-tpu-accent-hover)] transition-colors"
          >
            Подробнее о поступлении →
          </Link>
        </div>
      </div>
    </section>
  )
}
