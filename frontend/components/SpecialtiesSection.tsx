"use client"

import { Laptop, Radio, Cpu, Zap } from "lucide-react"

interface Specialty {
  code: string
  title: string
  qualifications: string[]
  duration: string
  icon: React.ReactNode
}

const specialties: Specialty[] = [
  {
    code: "09.02.07",
    title: "Информационные системы и программирование",
    qualifications: ["Программист", "Администратор баз данных", "Разработчик веб и мультимедийных приложений"],
    duration: "3 г 10 мес (9 кл)",
    icon: <Laptop size={32} />,
  },
  {
    code: "11.02.02",
    title: "Техническое обслуживание и ремонт радиоэлектронной техники",
    qualifications: ["Техник"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Radio size={32} />,
  },
  {
    code: "11.02.15",
    title: "Инфокоммуникационные сети и системы связи",
    qualifications: ["Специалист по обслуживанию телекоммуникаций", "Специалист по монтажу и обслуживанию телекоммуникаций"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Radio size={32} />,
  },
  {
    code: "11.02.17",
    title: "Разработка электронных устройств и систем",
    qualifications: ["Техник"],
    duration: "2 г 10 мес (9 кл) / 1 г 10 мес (11 кл)",
    icon: <Cpu size={32} />,
  },
  {
    code: "13.02.06",
    title: "Релейная защита и автоматизация электроэнергетических систем",
    qualifications: ["Техник-электрик"],
    duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)",
    icon: <Zap size={32} />,
  },
  {
    code: "13.02.12",
    title: "Электрические станции, сети, их релейная защита и автоматизация",
    qualifications: ["Техник-электрик"],
    duration: "3 г 10 мес (9 кл)",
    icon: <Zap size={32} />,
  },
]

export default function SpecialtiesSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Специальности</h2>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {specialties.map((s) => (
            <div
              key={s.code}
              className="rounded-lg border border-border bg-card p-6 transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
            >
              <div className="mb-4 flex items-center gap-3">
                <span className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  {s.icon}
                </span>
                <div>
                  <p className="text-xs text-muted-foreground">{s.code}</p>
                  <h3 className="text-sm font-semibold text-primary leading-tight">{s.title}</h3>
                </div>
              </div>
              <div className="mb-3 space-y-1">
                <p className="text-xs font-medium text-muted-foreground">Квалификация:</p>
                <ul className="list-inside list-disc text-xs text-muted-foreground">
                  {s.qualifications.map((q) => (
                    <li key={q}>{q}</li>
                  ))}
                </ul>
              </div>
              <p className="text-xs text-muted-foreground">
                <span className="font-medium">Срок обучения:</span> {s.duration}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
