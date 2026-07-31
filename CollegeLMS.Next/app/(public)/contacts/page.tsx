import type { Metadata } from "next"
import { MapPin, Phone, Mail, Clock } from "lucide-react"

export const metadata: Metadata = {
  title: "Контакты",
  description: "Контактная информация Ставропольского колледжа связи",
}

const contacts = [
  {
    icon: MapPin,
    label: "Адрес",
    value: "355000, г. Ставрополь, пр-д Черняховского, 3",
  },
  { icon: Phone, label: "Приёмная комиссия", value: "+7 (8652) 24-25-27" },
  { icon: Mail, label: "Email", value: "college@stvcc.ru" },
  { icon: Clock, label: "Часы работы", value: "Пн–Пт: 9:00 – 18:00" },
]

export default function ContactsPage() {
  return (
    <div className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h1 className="mb-8 text-3xl font-bold text-fg">Контакты</h1>

        <div className="grid gap-8 lg:grid-cols-2">
          <div className="flex flex-col justify-between gap-6">
            {contacts.map((item) => (
              <div key={item.label} className="flex items-start gap-4">
                <item.icon size={24} className="shrink-0 mt-0.5 text-accent" />
                <div>
                  <p className="text-sm text-muted-fg">{item.label}</p>
                  <p className="text-base font-medium text-fg">{item.value}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="h-full min-h-[400px] overflow-hidden rounded-lg border border-border">
            <iframe
              title="Карта"
              src="https://www.google.com/maps?q=%D0%93%D0%91%D0%9F%D0%9E%D0%A3+%D0%A1%D1%82%D0%B0%D0%B2%D1%80%D0%BE%D0%BF%D0%BE%D0%BB%D1%8C%D1%81%D0%BA%D0%B8%D0%B9+%D0%BA%D0%BE%D0%BB%D0%BB%D0%B5%D0%B4%D0%B6+%D1%81%D0%B2%D1%8F%D0%B7%D0%B8+%D0%B8%D0%BC%D0%B5%D0%BD%D0%B8+%D0%9F%D0%B5%D1%82%D1%80%D0%BE%D0%B2%D0%B0&z=15&output=embed"
              className="block h-full w-full"
              style={{ border: 0, minHeight: "400px" }}
              allowFullScreen
              loading="lazy"
            />
          </div>
        </div>
      </div>
    </div>
  )
}
