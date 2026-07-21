"use client"

import Link from "next/link"
import { Laptop, Radio, Cpu, Zap, ArrowRight } from "lucide-react"

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

export default function SpecialtiesSection() {
  return (
    <section className="py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 text-center">
          <h2 className="mb-3 text-3xl font-bold text-fg">Специальности</h2>
          <p className="text-base text-muted-foreground max-w-2xl mx-auto">
            Выберите свою будущую профессию среди востребованных направлений подготовки
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {specialties.map((s) => (
            <Link
              key={s.code}
              href={`/specialties/${s.slug}`}
              className="group relative flex flex-col rounded-xl border border-border bg-card transition-all duration-200 hover:shadow-lg hover:border-accent/30 hover:-translate-y-0.5 overflow-hidden"
            >
              {/* Top accent bar */}
              <div className="h-1 w-full bg-gradient-to-r from-accent to-accent/60" />

              <div className="flex flex-1 flex-col p-6">
                {/* Badge + Icon row */}
                <div className="mb-4 flex items-start justify-between">
                  <span className="inline-flex items-center rounded-full bg-accent/10 px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-accent">
                    СПО
                  </span>
                  <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-primary/5 text-primary group-hover:bg-accent/10 group-hover:text-accent transition-colors">
                    {s.icon}
                  </span>
                </div>

                {/* Title */}
                <h3 className="mb-3 text-base font-semibold text-fg leading-snug group-hover:text-accent transition-colors">
                  {s.title}
                </h3>

                {/* Duration */}
                <div className="mb-3">
                  <span className="inline-flex items-center gap-1 rounded-md bg-muted px-2.5 py-1 text-xs text-muted-foreground">
                    <span className="font-medium">{s.duration}</span>
                  </span>
                </div>

                {/* Qualifications */}
                <div className="mb-4 space-y-1">
                  <p className="text-xs font-medium text-muted-foreground">Квалификация:</p>
                  <ul className="space-y-0.5">
                    {s.qualifications.map((q) => (
                      <li key={q} className="flex items-start gap-1.5 text-xs text-fg">
                        <span className="mt-0.5 h-1.5 w-1.5 shrink-0 rounded-full bg-accent" />
                        {q}
                      </li>
                    ))}
                  </ul>
                </div>

                {/* Spacer */}
                <div className="mt-auto" />

                {/* Button */}
                <div className="flex items-center gap-1 text-sm font-medium text-accent opacity-0 group-hover:opacity-100 transition-opacity">
                  Подробнее
                  <ArrowRight size={15} className="transition-transform group-hover:translate-x-0.5" />
                </div>
              </div>
            </Link>
          ))}
        </div>

        <div className="mt-12 text-center">
          <Link
            href="/specialties"
            className="inline-flex items-center gap-2 rounded-lg bg-accent px-8 py-3.5 text-sm font-medium text-accent-foreground transition-all hover:bg-accent/90 hover:shadow-md"
          >
            Все специальности
            <ArrowRight size={16} />
          </Link>
        </div>
      </div>
    </section>
  )
}
