import { PageTitle, SubTitle, SectionTitle } from "@/components/ui/heading"
import Link from "next/link"

export default function NotFound() {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center px-4 text-center">
      <PageTitle className="mb-4 text-4xl">404</PageTitle>
      <p className="mb-6 text-lg text-muted-foreground">Страница не найдена</p>
      <Link
        href="/"
        className="rounded-md bg-accent px-6 py-2 text-sm font-medium text-accent-foreground transition-colors hover:bg-accent/90"
      >
        На главную
      </Link>
    </div>
  )
}
