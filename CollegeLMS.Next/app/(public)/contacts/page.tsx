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
          <div className="flex flex-col gap-6">
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

          <div className="overflow-hidden rounded-lg border border-border">
            <iframe
              title="Карта"
              src="https://yandex.ru/map-widget/v1/?ll=45.0450%2C41.9808&z=16&pt=45.0450%2C41.9808%2Cpm2dol&l=map"
              width="100%"
              height="400"
              style={{ border: 0 }}
              allowFullScreen
              loading="lazy"
              className="block"
            />
          </div>
        </div>
      </div>
    </div>
  )
}
