"use client"

import Link from "next/link"
import { Laptop, Radio, Cpu, Zap } from "lucide-react"

interface Specialty {
  code: string
  title: string
  qualifications: string[]
  duration: string
  icon: React.ReactNode
  slug: string
}

const specialties: Specialty[] = [
  {
    code: "09.02.07",
    title: "Информационные системы и программирование",
    qualifications: ["Программист", "Администратор баз данных", "Разработчик веб и мультимедийных приложений"],
    duration: "3 г 10 мес (9 кл)",
    icon: <Laptop size={32} />,
    slug: "informatsionnye-sistemy",
  },
  {
    code: "11.02.02",
    title: "Техническое обслуживание и ремонт радиоэлектронной техники",
    qualifications: ["Техник"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Radio size={32} />,
    slug: "remont-radioelektronnoj-tekhniki",
  },
  {
    code: "11.02.15",
    title: "Инфокоммуникационные сети и системы связи",
    qualifications: ["Специалист по обслуживанию телекоммуникаций", "Специалист по монтажу и обслуживанию телекоммуникаций"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Radio size={32} />,
    slug: "infokommunikatsionnye-seti",
  },
  {
    code: "11.02.17",
    title: "Разработка электронных устройств и систем",
    qualifications: ["Техник"],
    duration: "2 г 10 мес (9 кл) / 1 г 10 мес (11 кл)",
    icon: <Cpu size={32} />,
    slug: "razrabotka-elektronnykh-ustrojstv",
  },
  {
    code: "13.02.06",
    title: "Релейная защита и автоматизация электроэнергетических систем",
    qualifications: ["Техник-электрик"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Zap size={32} />,
    slug: "relejnaya-zashchita",
  },
  {
    code: "13.02.12",
    title: "Электрические станции, сети, их релейная защита и автоматизация",
    qualifications: ["Техник-электрик"],
    duration: "3 г 10 мес (9 кл)",
    icon: <Zap size={32} />,
    slug: "elektricheskie-stantsii",
  },
]

export default function SpecialtiesSectionTPU() {
  return (
    <div className="app-section">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <h2 className="app-section__title">Специальности</h2>
        <p className="app-section__subtitle">
          Мы готовим востребованных специалистов по 6 направлениям в сфере IT, связи и энергетики
        </p>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {specialties.map((s) => (
            <Link
              key={s.code}
              href="/education"
              className="group rounded-lg border border-tpu-border bg-white p-6 transition-all hover:shadow-md hover:border-tpu-blue/30"
            >
              <div className="mb-4 flex items-center gap-3">
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-tpu-blue-light text-tpu-blue">
                  {s.icon}
                </span>
                <div>
                  <p className="text-xs text-tpu-text-muted">{s.code}</p>
                  <h3 className="text-sm font-semibold text-tpu-text leading-tight">
                    {s.title}
                  </h3>
                </div>
              </div>
              <div className="mb-3 space-y-1">
                <p className="text-xs font-medium text-tpu-text-secondary">Квалификация:</p>
                <ul className="list-inside list-disc text-xs text-tpu-text-muted">
                  {s.qualifications.map((q) => (
                    <li key={q}>{q}</li>
                  ))}
                </ul>
              </div>
              <p className="text-xs text-tpu-text-muted">
                <span className="font-medium">Срок обучения:</span> {s.duration}
              </p>
            </Link>
          ))}
        </div>

        <div className="mt-10 text-center">
          <Link href="/education" className="btn-tpu-accent">
            Все специальности
          </Link>
        </div>
      </div>
    </div>
  )
}
