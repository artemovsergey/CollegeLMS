import { GraduationCap, Users, School, Building2, Award, TrendingUp } from "lucide-react"

const facts = [
  {
    icon: School,
    title: "6 специальностей",
    description: "Программирование, инфокоммуникации, радиоэлектроника, информационные системы и другие востребованные направления",
  },
  {
    icon: Users,
    title: "500+ студентов",
    description: "Ежегодно обучаются на очной и заочной формах. Выпускники работают в ведущих IT-компаниях региона",
  },
  {
    icon: GraduationCap,
    title: "50+ преподавателей",
    description: "Высококвалифицированные специалисты, в том числе с учёными степенями и отраслевыми наградами",
  },
  {
    icon: Building2,
    title: "15+ партнёров",
    description: "ОДК-Сатурн, Ростелеком, ЭР-Телеком, Военный университет радиоэлектроники — базы практики и стажировок",
  },
  {
    icon: TrendingUp,
    title: "85% трудоустройства",
    description: "Выпускники находят работу по специальности в течение первого года после окончания колледжа",
  },
]

export default function StatisticsSection() {
  return (
    <section className="bg-primary py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-2 text-center text-2xl font-semibold text-white">Колледж в цифрах</h2>
        <p className="mb-10 text-center text-sm text-white/70">
          Факты и достижения Ставропольского колледжа связи
        </p>
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {facts.map((f) => (
            <div key={f.title} className="rounded-lg bg-white/10 p-5">
              <span className="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-white/15 text-white">
                <f.icon size={20} />
              </span>
              <h3 className="mb-1 text-base font-semibold text-white">{f.title}</h3>
              <p className="text-sm leading-relaxed text-white/75">{f.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
