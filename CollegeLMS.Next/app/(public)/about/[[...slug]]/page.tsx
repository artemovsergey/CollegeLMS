import AboutPage from "@/components/AboutPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Сведения об образовательной организации",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <AboutPage slug={params.slug} />
}
