"use client"

import { redirect } from "next/navigation"
import { notFound } from "next/navigation"
import Breadcrumbs from "@/components/Breadcrumbs"
import BreadcrumbsTPU from "@/components/BreadcrumbsTPU"
import ContentRenderer from "@/components/ContentRenderer"
import DocsSidebar from "@/components/DocsSidebar"
import { getSectionBySlug } from "@/data/site-content"
import pageContents from "@/data/page-contents.json"
import { useDesign } from "@/lib/design-provider"

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
  const { design } = useDesign()
  const section = getSectionBySlug(sectionSlug)

  if (!section) {
    notFound()
    return null
  }

  if (!slug || slug.length === 0) {
    const first = section.subsections[0]
    if (first) redirect(first.href)
    notFound()
    return null
  }

  const subSlug = slug[0]
  const subsection = section.subsections.find((s) => s.slug === subSlug)

  if (!subsection) {
    notFound()
    return null
  }

  const rawContent = subSlug === "trudoustroystvo"
    ? (pageContents as Record<string, { content: string }>)["trudoustroystvo"]?.content || ""
    : getContent(subSlug) || subsection.content

  const Bc = design === "tpu" ? BreadcrumbsTPU : Breadcrumbs

  const titleClass = design === "tpu"
    ? "text-tpu-text"
    : "text-primary"
  const boxClass = design === "tpu"
    ? "rounded-lg border border-tpu-border bg-white p-8 text-center"
    : "rounded-lg border border-border bg-card p-8 text-center"
  const emptyClass = design === "tpu"
    ? "text-tpu-text-muted"
    : "text-muted-foreground"

  return (
    <div className="mx-auto flex max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <DocsSidebar
        sectionTitle={section.title}
        sectionHref={section.href}
        subsections={section.subsections}
        currentSlug={subSlug}
      />
      <div className="min-w-0 flex-1 pl-0 md:pl-8">
        <Bc
          items={[
            { label: section.title, href: section.href },
            { label: subsection.title },
          ]}
        />
        <h1 className={`mb-6 text-2xl font-bold ${titleClass}`}>{subsection.title}</h1>
        {rawContent ? (
          <ContentRenderer content={rawContent} />
        ) : (
          <div className={boxClass}>
            <p className={emptyClass}>Нет данных</p>
          </div>
        )}
      </div>
    </div>
  )
}
