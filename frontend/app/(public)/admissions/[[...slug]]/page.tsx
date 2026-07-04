import SectionPage from "@/components/SectionPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Абитуриенту",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <SectionPage sectionSlug="admissions" slug={params.slug} />
}
