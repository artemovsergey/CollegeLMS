import SectionPage from "@/components/SectionPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Достижения",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <SectionPage sectionSlug="achievements" slug={params.slug} />
}
