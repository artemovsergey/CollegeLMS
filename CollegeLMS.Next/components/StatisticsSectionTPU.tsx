"use client"

import { GraduationCap, Users, School, Building2, Award } from "lucide-react"

const stats = [
  { icon: School, value: "6", label: "Специальностей" },
  { icon: Users, value: "500+", label: "Студентов" },
  { icon: GraduationCap, value: "50+", label: "Преподавателей" },
  { icon: Building2, value: "15+", label: "Партнёров" },
  { icon: Award, value: "30", label: "Лет истории" },
]

export default function StatisticsSectionTPU() {
  return (
    <section className="bg-[var(--color-tpu-text-primary)] py-[var(--section-padding-y)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 text-center">
          <h2 className="mb-3 text-3xl font-bold text-white">
            Колледж в цифрах
          </h2>
          <p className="text-white/60 max-w-2xl mx-auto">
            Более 30 лет мы готовим специалистов для IT и телекоммуникационной отрасли
          </p>
        </div>
        <div className="grid grid-cols-2 gap-8 md:grid-cols-5">
          {stats.map((s) => (
            <div key={s.label} className="flex flex-col items-center text-center">
              <span className="mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-white/10 text-[var(--color-tpu-accent)]">
                <s.icon size={28} />
              </span>
              <span className="text-3xl font-bold text-white">{s.value}</span>
              <span className="mt-1 text-sm text-white/60">{s.label}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
