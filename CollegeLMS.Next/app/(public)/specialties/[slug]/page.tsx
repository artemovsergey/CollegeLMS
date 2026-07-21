import Link from "next/link"
import { notFound } from "next/navigation"
import { Metadata } from "next"
import { Laptop, Radio, Cpu, Zap, ArrowLeft, GraduationCap, Clock, Award } from "lucide-react"

const specialties = [
  { code: "09.02.07", title: "Информационные системы и программирование", qualifications: ["Программист", "Администратор баз данных", "Разработчик веб и мультимедийных приложений"], duration: "3 года 10 месяцев (9 классов)", durationShort: "3 г 10 мес (9 кл)", icon: Laptop, slug: "informatsionnye-sistemy", description: "Специалист в области информационных систем занимается разработкой, внедрением и сопровождением программного обеспечения, баз данных и веб-приложений. Выпускники востребованы в IT-компаниях, телекоммуникационных и промышленных предприятиях.",
    form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
  { code: "11.02.02", title: "Техническое обслуживание и ремонт радиоэлектронной техники", qualifications: ["Техник"], duration: "3 года 10 месяцев (9 классов) / 2 года 10 месяцев (11 классов)", durationShort: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Radio, slug: "remont-radioelektronnoj-tekhniki", description: "Техник по радиоэлектронной технике осуществляет техническое обслуживание, диагностику и ремонт радиоэлектронных устройств и систем различного назначения.", form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
  { code: "11.02.15", title: "Инфокоммуникационные сети и системы связи", qualifications: ["Специалист по обслуживанию телекоммуникаций", "Специалист по монтажу и обслуживанию телекоммуникаций"], duration: "3 года 10 месяцев (9 классов) / 2 года 10 месяцев (11 классов)", durationShort: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Radio, slug: "infokommunikatsionnye-seti", description: "Специалист в области инфокоммуникаций занимается проектированием, монтажом и обслуживанием сетей связи, телекоммуникационного оборудования и систем передачи данных.", form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
  { code: "11.02.17", title: "Разработка электронных устройств и систем", qualifications: ["Техник"], duration: "2 года 10 месяцев (9 классов) / 1 год 10 месяцев (11 классов)", durationShort: "2 г 10 мес (9 кл) / 1 г 10 мес (11 кл)", icon: Cpu, slug: "razrabotka-elektronnykh-ustrojstv", description: "Техник по разработке электронных устройств занимается проектированием, сборкой и настройкой электронных схем, микропроцессорных систем и устройств автоматики.", form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
  { code: "13.02.06", title: "Релейная защита и автоматизация электроэнергетических систем", qualifications: ["Техник-электрик"], duration: "3 года 10 месяцев (9 классов) / 2 года 10 месяцев (11 классов)", durationShort: "3 г 10 мес (9 кл) / 2 г 10 мес (11 кл)", icon: Zap, slug: "relejnaya-zashchita", description: "Техник-электрик в области релейной защиты занимается эксплуатацией, наладкой и обслуживанием устройств релейной защиты и автоматики электроэнергетических систем.", form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
  { code: "13.02.12", title: "Электрические станции, сети, их релейная защита и автоматизация", qualifications: ["Техник-электрик"], duration: "3 года 10 месяцев (9 классов)", durationShort: "3 г 10 мес (9 кл)", icon: Zap, slug: "elektricheskie-stantsii", description: "Специалист в области электрических станций и сетей занимается эксплуатацией, обслуживанием и ремонтом электрооборудования электрических станций и сетей.", form: "Очная", budget: "Бюджетные места", level: "Среднее профессиональное образование" },
]

export function generateStaticParams() {
  return specialties.map((s) => ({ slug: s.slug }))
}

export function generateMetadata({ params }: { params: { slug: string } }): Metadata {
  const spec = specialties.find((s) => s.slug === params.slug)
  if (!spec) return { title: "Специальность не найдена" }
  return {
    title: `${spec.title} | Ставропольский колледж связи`,
    description: spec.description,
  }
}

export default function SpecialtyDetailPage({ params }: { params: { slug: string } }) {
  const spec = specialties.find((s) => s.slug === params.slug)
  if (!spec) notFound()

  const Icon = spec.icon

  return (
    <div className="py-16">
      <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        {/* Breadcrumb */}
        <Link href="/specialties" className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-accent transition-colors mb-8">
          <ArrowLeft size={16} />
          К списку специальностей
        </Link>

        {/* Hero section */}
        <div className="mb-10">
          <span className="inline-flex items-center rounded-full bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-accent mb-4">
            {spec.code}
          </span>

          <div className="flex items-start gap-5 mb-4">
            <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-xl bg-accent/10 text-accent">
              <Icon size={32} />
            </span>
            <div>
              <h1 className="text-2xl lg:text-3xl font-bold text-fg leading-tight">{spec.title}</h1>
              <p className="mt-1 text-sm text-muted-foreground">{spec.level}</p>
            </div>
          </div>

          {/* Tags */}
          <div className="flex flex-wrap gap-2 mt-4">
            <span className="inline-flex items-center gap-1.5 rounded-md bg-accent/10 px-3 py-1.5 text-xs font-medium text-accent">
              <Clock size={14} />
              {spec.durationShort}
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-md bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary">
              <GraduationCap size={14} />
              {spec.form}
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-md bg-green-100 dark:bg-green-900/30 px-3 py-1.5 text-xs font-medium text-green-700 dark:text-green-400">
              <Award size={14} />
              {spec.budget}
            </span>
          </div>
        </div>

        {/* Description */}
        <div className="prose prose-sm dark:prose-invert max-w-none mb-10">
          <h2 className="text-lg font-semibold text-fg">О специальности</h2>
          <p className="text-muted-foreground leading-relaxed">{spec.description}</p>
        </div>

        {/* Qualifications */}
        <div className="mb-10 rounded-xl border border-border bg-card p-6">
          <h2 className="mb-4 text-lg font-semibold text-fg">Квалификация</h2>
          <ul className="space-y-2">
            {spec.qualifications.map((q) => (
              <li key={q} className="flex items-start gap-3 text-sm text-fg">
                <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-accent" />
                {q}
              </li>
            ))}
          </ul>
        </div>

        {/* Duration */}
        <div className="mb-10 rounded-xl border border-border bg-card p-6">
          <h2 className="mb-4 text-lg font-semibold text-fg">Срок обучения</h2>
          <p className="text-sm text-muted-foreground">{spec.duration}</p>
        </div>

        {/* CTA */}
        <div className="rounded-xl bg-accent/5 border border-accent/20 p-8 text-center">
          <h2 className="mb-2 text-xl font-semibold text-fg">Готовы начать обучение?</h2>
          <p className="mb-6 text-sm text-muted-foreground">
            Подайте заявление на поступление или получите консультацию приёмной комиссии
          </p>
          <div className="flex flex-wrap items-center justify-center gap-4">
            <Link
              href="/admissions"
              className="inline-flex items-center gap-2 rounded-lg bg-accent px-6 py-3 text-sm font-medium text-accent-foreground transition-all hover:bg-accent/90 hover:shadow-md"
            >
              Подать заявление
              <ArrowLeft size={16} className="rotate-180" />
            </Link>
            <Link
              href="/contacts"
              className="inline-flex items-center gap-2 rounded-lg border border-border bg-card px-6 py-3 text-sm font-medium text-fg transition-all hover:bg-muted"
            >
              Связаться с нами
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
