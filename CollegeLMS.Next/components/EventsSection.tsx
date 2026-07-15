import { CalendarDays, MapPin, Clock } from "lucide-react"
import Link from "next/link"

const events = [
  {
    date: "15 августа 2026",
    title: "Окончание приёма документов (очная форма)",
    description: "Последний день подачи документов на очную форму обучения",
    location: "пер. Черняховского, 3",
  },
  {
    date: "20 августа 2026",
    title: "Зачисление на бюджетные места",
    description: "Издание приказа о зачислении абитуриентов на бюджетные места",
    location: "Колледж связи",
  },
  {
    date: "1 сентября 2026",
    title: "День знаний — Торжественная линейка",
    description: "Торжественное мероприятие, посвящённое началу учебного года",
    location: "пер. Черняховского, 3",
  },
]

export default function EventsSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-8 flex items-center justify-between">
          <h2 className="text-2xl font-semibold text-primary">Календарь событий</h2>
          <Link href="/events" className="text-sm font-medium text-accent hover:text-accent/80">
            Все события →
          </Link>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {events.map((event) => (
            <div
              key={event.title}
              className="rounded-lg border border-border bg-card p-5 transition-all duration-200 hover:shadow-sm"
            >
              <p className="mb-2 text-xs font-medium text-accent">{event.date}</p>
              <h3 className="mb-2 text-sm font-semibold text-primary">{event.title}</h3>
              <p className="mb-3 text-xs leading-relaxed text-muted-foreground">{event.description}</p>
              <div className="flex items-center gap-3 text-xs text-muted-foreground">
                <span className="flex items-center gap-1">
                  <MapPin size={12} />
                  {event.location}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
