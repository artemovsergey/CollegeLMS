import SectionPage from "@/components/SectionPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Выпускнику",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <SectionPage sectionSlug="graduates" slug={params.slug} />
}
