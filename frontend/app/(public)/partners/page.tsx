import { type Metadata } from "next"
import Breadcrumbs from "@/components/Breadcrumbs"
import { partnersContent } from "@/data/site-content"

export const metadata: Metadata = {
  title: "Наши партнёры",
}

export default function PartnersPage() {
  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <Breadcrumbs items={[{ label: "Наши партнёры" }]} />
      <h1 className="mb-6 text-2xl font-bold text-foreground">Наши партнёры</h1>
      <div
        className="prose prose-sm max-w-none text-muted-foreground leading-relaxed"
        dangerouslySetInnerHTML={{ __html: partnersContent }}
      />
    </div>
  )
}
