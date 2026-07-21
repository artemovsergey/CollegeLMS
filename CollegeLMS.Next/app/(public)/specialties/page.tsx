import { Metadata } from "next"
import Link from "next/link"
import { Laptop, Radio, Cpu, Zap, ArrowRight } from "lucide-react"

export const metadata: Metadata = {
  title: "Специальности | Ставропольский колледж связи",
  description: "Специальности и профессии колледжа",
}

const specialtiesPage = [
  { code: "09.02.07", title: "Информационные системы и программирование", qualifications: ["Программист", "Администратор баз данных", "Разработчик веб и мультимедийных приложений"], duration: "3 г 10 мес (9 кл)", icon: Laptop, slug: "informatsionnye-sistemy" },
  { code: "11.02.02", title: "Техническое обслуживание и ремонт радиоэлектронной техники", qualifications: ["Техник"], duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Radio, slug: "remont-radioelektronnoj-tekhniki" },
  { code: "11.02.15", title: "Инфокоммуникационные сети и системы связи", qualifications: ["Специалист по обслуживанию телекоммуникаций", "Специалист по монтажу и обслуживанию телекоммуникаций"], duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Radio, slug: "infokommunikatsionnye-seti" },
  { code: "11.02.17", title: "Разработка электронных устройств и систем", qualifications: ["Техник"], duration: "2 г 10 мес (9 кл) / 1 г 10 мес (11 кл)", icon: Cpu, slug: "razrabotka-elektronnykh-ustrojstv" },
  { code: "13.02.06", title: "Релейная защита и автоматизация электроэнергетических систем", qualifications: ["Техник-электрик"], duration: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Zap, slug: "relejnaya-zashchita" },
  { code: "13.02.12", title: "Электрические станции, сети, их релейная защита и автоматизация", qualifications: ["Техник-электрик"], duration: "3 г 10 мес (9 кл)", icon: Zap, slug: "elektricheskie-stantsii" },
]

export default function SpecialtiesPage() {
  return (
    <div className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-12 text-center">
          <h1 className="mb-3 text-3xl font-bold text-fg">Специальности и профессии</h1>
          <p className="text-base text-muted-foreground max-w-2xl mx-auto">
            Колледж ведёт подготовку по востребованным направлениям в сфере IT, связи, электроэнергетики и радиоэлектроники
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {specialtiesPage.map((s) => {
            const Icon = s.icon
            return (
              <Link
                key={s.code}
                href={`/specialties/${s.slug}`}
                className="group relative flex flex-col rounded-xl border border-border bg-card transition-all duration-200 hover:shadow-lg hover:border-accent/30 hover:-translate-y-0.5 overflow-hidden"
              >
                <div className="h-1 w-full bg-gradient-to-r from-accent to-accent/60" />

                <div className="flex flex-1 flex-col p-6">
                  <div className="mb-4 flex items-start justify-between">
                    <span className="inline-flex items-center rounded-full bg-accent/10 px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-accent">СПО</span>
                    <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-primary/5 text-primary group-hover:bg-accent/10 group-hover:text-accent transition-colors">
                      <Icon size={24} />
                    </span>
                  </div>

                  <h3 className="mb-3 text-base font-semibold text-fg leading-snug group-hover:text-accent transition-colors">{s.title}</h3>

                  <div className="mb-3">
                    <span className="inline-flex items-center gap-1 rounded-md bg-muted px-2.5 py-1 text-xs text-muted-foreground">
                      <span className="font-medium">{s.duration}</span>
                    </span>
                  </div>

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

                  <div className="mt-auto" />

                  <div className="flex items-center gap-1 text-sm font-medium text-accent opacity-0 group-hover:opacity-100 transition-opacity">
                    Подробнее
                    <ArrowRight size={15} className="transition-transform group-hover:translate-x-0.5" />
                  </div>
                </div>
              </Link>
            )
          })}
        </div>
      </div>
    </div>
  )
}
