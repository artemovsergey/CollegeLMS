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
    <div className="app-section app-section--alt">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <h2 className="app-section__title">Приёмная кампания 2026</h2>
        <p className="app-section__subtitle">Информация для абитуриентов и их родителей</p>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div
              key={item.title}
              className="flex gap-4 rounded-lg border border-tpu-border bg-white p-5 transition-shadow hover:shadow-sm"
            >
              <span className="mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-tpu-blue-light text-tpu-blue">
                <item.icon size={20} />
              </span>
              <div>
                <h3 className="mb-1 text-sm font-semibold text-tpu-text">{item.title}</h3>
                <p className="whitespace-pre-line text-xs leading-relaxed text-tpu-text-muted">
                  {item.description}
                </p>
              </div>
            </div>
          ))}
        </div>

        <div className="mt-10 text-center">
          <Link href="/admissions" className="btn-tpu-accent">
            Подробнее о поступлении
          </Link>
        </div>
      </div>
    </div>
  )
}
