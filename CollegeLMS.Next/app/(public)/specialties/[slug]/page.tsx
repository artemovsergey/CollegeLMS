import { PageTitle, SubTitle, SectionTitle } from "@/components/ui/heading"
import Link from "next/link"
import { notFound } from "next/navigation"
import { Metadata } from "next"
import { ArrowLeft, GraduationCap, Clock, Award } from "lucide-react"
import { specialties } from "@/data/specialties"

export function generateStaticParams() {
  return specialties.map((s) => ({ slug: s.slug }))
}

export function generateMetadata({ params }: { params: { slug: string } }): Metadata {
  const spec = specialties.find((s) => s.slug === params.slug)
  if (!spec) return { title: "Специальность не найдена" }
  return {
    title: `${spec.title} | Ставропольский колледж связи`,
    description: spec.description,
  }
}

export default function SpecialtyDetailPage({ params }: { params: { slug: string } }) {
  const spec = specialties.find((s) => s.slug === params.slug)
  if (!spec) notFound()

  const Icon = spec.icon

  return (
    <div className="py-16">
      <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        {/* Breadcrumb */}
        <Link href="/specialties" className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-accent transition-colors mb-8">
          <ArrowLeft size={16} />
          К списку специальностей
        </Link>

        {/* Hero section */}
        <div className="mb-10">
          <span className="inline-flex items-center rounded-full bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-accent mb-4">
            {spec.code}
          </span>

          <div className="flex items-start gap-5 mb-4">
            <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-xl bg-accent/10 text-accent">
              <Icon size={32} />
            </span>
            <div>
              <PageTitle className="leading-tight">{spec.title}</PageTitle>
              <p className="mt-1 text-sm text-muted-foreground">{spec.level}</p>
            </div>
          </div>

          {/* Tags */}
          <div className="flex flex-wrap gap-2 mt-4">
            <span className="inline-flex items-center gap-1.5 rounded-md bg-accent/10 px-3 py-1.5 text-xs font-medium text-accent">
              <Clock size={14} />
              {spec.durationShort}
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-md bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary">
              <GraduationCap size={14} />
              {spec.form}
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-md bg-success/10 px-3 py-1.5 text-xs font-medium text-success-text">
              <Award size={14} />
              {spec.budget}
            </span>
          </div>
        </div>

        {/* Description */}
        <div className="prose prose-sm prose-narrow mb-10">
          <SubTitle>О специальности</SubTitle>
          <p className="text-muted-foreground leading-relaxed">{spec.description}</p>
        </div>

        {/* Qualifications */}
        <div className="mb-10 rounded-xl border border-border bg-card p-6">
          <SubTitle className="mb-4">Квалификация</SubTitle>
          <ul className="space-y-2">
            {spec.qualifications.map((q) => (
              <li key={q} className="flex items-start gap-3 text-sm text-fg">
                <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-accent" />
                {q}
              </li>
            ))}
          </ul>
        </div>

        {/* Duration */}
        <div className="mb-10 rounded-xl border border-border bg-card p-6">
          <SubTitle className="mb-4">Срок обучения</SubTitle>
          <p className="text-sm text-muted-foreground">{spec.duration}</p>
        </div>

      </div>
    </div>
  )
}
