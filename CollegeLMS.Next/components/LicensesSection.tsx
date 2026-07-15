import { FileText, Award, ScrollText } from "lucide-react"
import Link from "next/link"

const docs = [
  {
    icon: FileText,
    title: "Лицензия на осуществление образовательной деятельности",
    description: "Серия 26Л01 № 0001234, бессрочная",
    href: "#",
  },
  {
    icon: Award,
    title: "Свидетельство о государственной аккредитации",
    description: "Серия 26А01 № 0000567, действительно до 2028 г.",
    href: "#",
  },
  {
    icon: ScrollText,
    title: "Устав колледжа",
    description: "Утверждён Министерством энергетики, промышленности и связи СК",
    href: "#",
  },
]

export default function LicensesSection() {
  return (
    <section className="bg-muted py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="mb-8 text-center text-2xl font-semibold text-primary">Лицензии и документы</h2>
        <div className="mx-auto grid max-w-3xl gap-4">
          {docs.map((doc) => (
            <Link
              key={doc.title}
              href={doc.href}
              className="flex items-center gap-4 rounded-lg border border-border bg-card p-5 transition-all duration-200 hover:border-accent/30 hover:shadow-sm"
            >
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <doc.icon size={24} />
              </span>
              <div>
                <h3 className="text-sm font-semibold text-primary">{doc.title}</h3>
                <p className="text-xs text-muted-foreground">{doc.description}</p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </section>
  )
}
