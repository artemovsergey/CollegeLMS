"use client"

import Link from "next/link"
import { ArrowRight } from "lucide-react"
import { specialties } from "@/data/specialties"

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
              className="group relative flex flex-col rounded-xl border border-border bg-card transition-all duration-200 hover:border-accent/30 hover:-translate-y-0.5 overflow-hidden"
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
                    <s.icon size={24} />
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
                <div className="flex items-center gap-1 text-sm font-medium text-accent">
                  <span className="rounded focus-visible:outline-none">Подробнее</span>
                  <ArrowRight size={15} className="transition-transform group-hover:translate-x-0.5" />
                </div>
              </div>
            </Link>
          ))}
        </div>

        <div className="mt-12 text-center">
          <Link
            href="/specialties"
            className="inline-flex items-center gap-2 rounded-lg bg-accent px-8 py-3.5 text-sm font-medium text-accent-foreground transition-all hover:bg-accent/90"
          >
            Все специальности
            <ArrowRight size={16} />
          </Link>
        </div>
      </div>
    </section>
  )
}
