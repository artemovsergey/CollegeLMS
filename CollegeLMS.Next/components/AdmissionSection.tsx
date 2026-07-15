import { CalendarDays, MapPin, Phone, Building, ShieldCheck, Bed } from "lucide-react"

const items = [
  {
    icon: CalendarDays,
    title: "Приём документов",
    description: "Очная форма: 19 июня — 15 августа 2026\nЗаочная форма: 19 июня — 1 декабря 2026",
  },
  {
    icon: MapPin,
    title: "Адрес",
    description: "355031, г. Ставрополь, пер. Черняховского, 3",
  },
  {
    icon: Phone,
    title: "Приёмная комиссия",
    description: "24-20-32\nОтветственный секретарь: +7 (918) 744-74-70",
  },
  {
    icon: Building,
    title: "Заочное отделение",
    description: "24-23-33",
  },
  {
    icon: ShieldCheck,
    title: "Отсрочка от армии",
    description: "Всем зачисленным на очную форму обучения предоставляется отсрочка до получения диплома",
  },
  {
    icon: Bed,
    title: "Общежитие",
    description: "Поступившим при необходимости предоставляется общежитие",
  },
]

export default function AdmissionSection() {
  return (
    <section className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Приёмная кампания 2026</h2>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div key={item.title} className="flex gap-4 rounded-lg border border-border bg-card p-5">
              <span className="mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <item.icon size={20} />
              </span>
              <div>
                <h3 className="mb-1 text-sm font-semibold text-primary">{item.title}</h3>
                <p className="whitespace-pre-line text-xs leading-relaxed text-muted-foreground">{item.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
