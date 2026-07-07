import { Quote } from "lucide-react"

const graduates = [
  {
    name: "Анна К.",
    program: "Информационные системы и программирование",
    year: "2022",
    quote: "Колледж дал мне не только знания, но и уверенность в своих силах. Преподаватели — настоящие профессионалы, которые вкладывают душу в своё дело. Уже через месяц после выпуска я устроилась программистом в IT-компанию.",
    role: "Программист, ПАО «Ростелеком»",
    initials: "АК",
  },
  {
    name: "Дмитрий С.",
    program: "Техническое обслуживание и ремонт радиоэлектронной техники",
    year: "2021",
    quote: "Практическая направленность обучения — главное преимущество колледжа. Мы работали с реальным оборудованием, участвовали в конкурсах профмастерства. После армии вернулся на завод уже старшим техником.",
    role: "Старший техник, ОДК-Сатурн",
    initials: "ДС",
  },
  {
    name: "Елена В.",
    program: "Инфокоммуникационные сети и системы связи",
    year: "2023",
    quote: "Учиться было интересно с первого курса: современные лаборатории, практика у партнёров, хакатоны. Сейчас работаю инженером связи и параллельно учусь в вузе по ускоренной программе — колледж дал отличную базу.",
    role: "Инженер связи, АО «ЭР-Телеком»",
    initials: "ЕВ",
  },
]

export default function GraduateStoriesSection() {
  return (
    <section className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Истории выпускников</h2>
        <div className="grid gap-6 md:grid-cols-3">
          {graduates.map((g) => (
            <div
              key={g.name}
              className="relative flex flex-col rounded-lg border border-border bg-card p-6 transition-all duration-200 hover:shadow-sm"
            >
              <span className="mb-4 flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                <Quote size={18} />
              </span>
              <p className="mb-4 text-sm leading-relaxed text-muted-foreground">{g.quote}</p>
              <div className="mt-auto flex items-center gap-3 border-t border-border pt-4">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-accent/10 text-sm font-semibold text-accent">
                  {g.initials}
                </span>
                <div>
                  <p className="text-sm font-semibold text-primary">{g.name}</p>
                  <p className="text-xs text-muted-foreground">{g.role}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
