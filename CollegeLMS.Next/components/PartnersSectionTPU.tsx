"use client"

import { Factory, GraduationCap, Building2, Radio, Cpu } from "lucide-react"

const partners = [
  {
    name: "ПАО «ОДК-Сатурн»",
    description: "Предприятие авиационного двигателестроения, партнёр в подготовке кадров",
    icon: Factory,
  },
  {
    name: "Военный университет радиоэлектроники",
    description: "Высшее учебное заведение, стратегический партнёр в области радиоэлектроники",
    icon: GraduationCap,
  },
  {
    name: "Министерство энергетики, промышленности и связи СК",
    description: "Учредитель колледжа, курирующий образовательную деятельность",
    icon: Building2,
  },
  {
    name: "ПАО «Ростелеком»",
    description: "Крупнейший провайдер цифровых услуг, база практики студентов",
    icon: Radio,
  },
  {
    name: "АО «ЭР-Телеком»",
    description: "Телекоммуникационная компания, партнёр в сфере IT и связи",
    icon: Cpu,
  },
]

export default function PartnersSectionTPU() {
  return (
    <section className="py-[var(--section-padding-y)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 text-center">
          <h2 className="mb-3 text-3xl font-bold text-[var(--color-tpu-text-primary)]">
            Наши партнёры
          </h2>
          <p className="text-[var(--color-tpu-text-secondary)] max-w-2xl mx-auto">
            Мы сотрудничаем с ведущими предприятиями и организациями региона
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {partners.map((p) => (
            <div
              key={p.name}
              className="flex flex-col items-center rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] p-6 text-center transition-all duration-200 hover:shadow-[var(--shadow-tpu-md)] hover:border-[var(--color-tpu-accent)]/30"
            >
              <span className="mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-[var(--color-tpu-accent-light)] text-[var(--color-tpu-accent)]">
                <p.icon size={28} />
              </span>
              <h3 className="mb-1 text-sm font-semibold text-[var(--color-tpu-text-primary)]">
                {p.name}
              </h3>
              <p className="text-xs text-[var(--color-tpu-text-secondary)]">
                {p.description}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
