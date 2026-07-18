"use client"

import { redirect } from "next/navigation"
import { notFound } from "next/navigation"
import BreadcrumbsTPU from "@/components/BreadcrumbsTPU"
import ContentRenderer from "@/components/ContentRenderer"
import DocsSidebar from "@/components/DocsSidebar"
import { getSectionBySlug } from "@/data/site-content"
import pageContents from "@/data/page-contents.json"

interface SectionPageTPUProps {
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

export default function SectionPageTPU({ sectionSlug, slug }: SectionPageTPUProps) {
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

  const rawContent =
    subSlug === "trudoustroystvo"
      ? (pageContents as Record<string, { content: string }>)["trudoustroystvo"]
          ?.content || ""
      : getContent(subSlug) || subsection.content

  return (
    <div className="mx-auto flex max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <DocsSidebar
        sectionTitle={section.title}
        sectionHref={section.href}
        subsections={section.subsections}
        currentSlug={subSlug}
      />
      <div className="min-w-0 flex-1 pl-0 md:pl-8">
        <BreadcrumbsTPU
          items={[
            { label: section.title, href: section.href },
            { label: subsection.title },
          ]}
        />
        <h1 className="mb-6 text-2xl font-bold text-[var(--color-tpu-text-primary)]">
          {subsection.title}
        </h1>
        {rawContent ? (
          <ContentRenderer content={rawContent} />
        ) : (
          <div className="rounded-xl border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] p-8 text-center">
            <p className="text-[var(--color-tpu-text-secondary)]">Нет данных</p>
          </div>
        )}
      </div>
    </div>
  )
}
