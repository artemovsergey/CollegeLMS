import { type Metadata } from "next"
import Breadcrumbs from "@/components/Breadcrumbs"
import ContentRenderer from "@/components/ContentRenderer"
import { partnersContent } from "@/data/site-content"

export const metadata: Metadata = {
  title: "Наши партнёры",
}

export default function PartnersPage() {
  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <Breadcrumbs items={[{ label: "Наши партнёры" }]} />
      <h1 className="mb-6 text-2xl font-bold text-primary">Наши партнёры</h1>
      <ContentRenderer content={partnersContent} />
    </div>
  )
}
