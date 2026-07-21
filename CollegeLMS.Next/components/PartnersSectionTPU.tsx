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
    <div className="app-section app-section--alt">
      <div className="mx-auto max-w-7xl px-4 lg:px-8">
        <h2 className="app-section__title">Наши партнёры</h2>
        <p className="app-section__subtitle">
          Мы сотрудничаем с ведущими предприятиями и организациями региона
        </p>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {partners.map((p) => (
            <div
              key={p.name}
              className="flex flex-col items-center rounded-lg border border-tpu-border bg-white p-6 text-center transition-all hover:shadow-md hover:border-tpu-blue/30"
            >
              <span className="mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-tpu-blue-light text-tpu-blue">
                <p.icon size={28} />
              </span>
              <h3 className="mb-1 text-sm font-semibold text-tpu-text">{p.name}</h3>
              <p className="text-xs text-tpu-text-muted">{p.description}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
