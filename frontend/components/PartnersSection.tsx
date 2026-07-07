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

export default function PartnersSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Наши партнёры</h2>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {partners.map((p) => (
            <div
              key={p.name}
              className="flex flex-col items-center rounded-lg border border-border bg-card p-6 text-center transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
            >
              <span className="mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-primary">
                <p.icon size={28} />
              </span>
              <h3 className="mb-1 text-sm font-semibold text-primary">{p.name}</h3>
              <p className="text-xs text-muted-foreground">{p.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
