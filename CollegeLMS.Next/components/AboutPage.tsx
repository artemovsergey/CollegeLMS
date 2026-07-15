import Breadcrumbs from "@/components/Breadcrumbs"
import ContentRenderer from "@/components/ContentRenderer"
import { notFound } from "next/navigation"
import pageContents from "@/data/page-contents.json"

interface AboutPageProps {
  slug?: string[]
}

const aboutSubsections = [
  { title: "Основные сведения", slug: "common" },
  { title: "Структура и органы управления", slug: "struct" },
  { title: "Документы", slug: "document" },
  { title: "Образование", slug: "education" },
  { title: "Образовательные стандарты и требования", slug: "edustandarts" },
  { title: "Руководство", slug: "rucovodstvo" },
  { title: "Педагогический состав", slug: "teachingstaff" },
  { title: "Материально-техническое обеспечение и оснащённость образовательного процесса", slug: "objects" },
  { title: "Стипендии и иные виды материальной поддержки", slug: "grants" },
  { title: "Платные образовательные услуги", slug: "paid_edu" },
  { title: "Финансово-хозяйственная деятельность", slug: "budget" },
  { title: "Вакантные места для приёма (перевода)", slug: "vacant" },
  { title: "Организация питания в образовательной организации", slug: "meals" },
  { title: "Международное сотрудничество", slug: "inter" },
  { title: "Свидетельство об аккредитации", slug: "svidetelstvo-ob-akkreditatsii" },
]

function getContent(slug: string): string {
  const key = slug as keyof typeof pageContents
  const entry = pageContents[key]
  if (entry && typeof entry === "object" && "content" in entry) {
    return (entry as { content: string }).content
  }
  return ""
}

export default function AboutPage({ slug }: AboutPageProps) {
  if (!slug || slug.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <Breadcrumbs items={[{ label: "Сведения об образовательной организации" }]} />
        <h1 className="mb-6 text-2xl font-bold text-primary">
          Сведения об образовательной организации
        </h1>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {aboutSubsections.map((sub) => (
            <a
              key={sub.slug}
              href={`/about/${sub.slug}`}
              className="rounded-lg border border-border bg-card p-5 transition-all hover:border-accent/30 hover:shadow-sm"
            >
              <h3 className="text-sm font-semibold text-primary">{sub.title}</h3>
            </a>
          ))}
        </div>
      </div>
    )
  }

  const subSlug = slug[0]
  const subsection = aboutSubsections.find((s) => s.slug === subSlug)

  if (!subsection) notFound()

  const rawContent = getContent(subSlug)

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <Breadcrumbs
        items={[
          { label: "Сведения об образовательной организации", href: "/about" },
          { label: subsection.title },
        ]}
      />
      <h1 className="mb-6 text-2xl font-bold text-primary">{subsection.title}</h1>
      {rawContent ? (
        <ContentRenderer content={rawContent} />
      ) : (
        <div className="rounded-lg border border-border bg-card p-8 text-center">
          <p className="text-muted-foreground">Содержание раздела загружается...</p>
        </div>
      )}
    </div>
  )
}
