import Breadcrumbs from "@/components/Breadcrumbs"
import { getSectionBySlug } from "@/data/site-content"
import { notFound } from "next/navigation"
import pageContents from "@/data/page-contents.json"

interface SectionPageProps {
  sectionSlug: string
  slug?: string[]
}

function getContent(slug: string): string {
  const key = slug as keyof typeof pageContents
  const entry = pageContents[key]
  if (entry && typeof entry === "object" && "content" in entry) {
    return (entry as { content: string }).content
  }
  return ""
}

export default function SectionPage({ sectionSlug, slug }: SectionPageProps) {
  const section = getSectionBySlug(sectionSlug)

  if (!section) notFound()

  if (!slug || slug.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <Breadcrumbs items={[{ label: section.title }]} />
        <h1 className="mb-6 text-2xl font-bold text-foreground">{section.title}</h1>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {section.subsections.map((sub) => (
            <a
              key={sub.slug}
              href={sub.href}
              className="rounded-lg border border-border bg-card p-5 transition-all hover:border-primary/30 hover:shadow-sm"
            >
              <h3 className="text-sm font-semibold text-foreground">{sub.title}</h3>
            </a>
          ))}
        </div>
      </div>
    )
  }

  const subSlug = slug[0]
  const subsection = section.subsections.find((s) => s.slug === subSlug)

  if (!subsection) notFound()

  const rawContent = getContent(subSlug) || subsection.content
  const cleanContent = rawContent
    .replace(/<img[^>]*>/gi, "")
    .replace(/<a[^>]*>/gi, "")
    .replace(/<\/a>/gi, "")

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <Breadcrumbs
        items={[
          { label: section.title, href: section.href },
          { label: subsection.title },
        ]}
      />
      <h1 className="mb-6 text-2xl font-bold text-foreground">{subsection.title}</h1>
      {cleanContent ? (
        <div
          className="prose prose-sm max-w-none text-muted-foreground leading-relaxed"
          dangerouslySetInnerHTML={{ __html: cleanContent }}
        />
      ) : (
        <div className="rounded-lg border border-border bg-card p-8 text-center">
          <p className="text-muted-foreground">Содержание раздела загружается...</p>
        </div>
      )}
    </div>
  )
}
