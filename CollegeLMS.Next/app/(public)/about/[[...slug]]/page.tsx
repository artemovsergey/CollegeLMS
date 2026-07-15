import SectionPage from "@/components/SectionPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Сведения об образовательной организации",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <SectionPage sectionSlug="about" slug={params.slug} />
}
