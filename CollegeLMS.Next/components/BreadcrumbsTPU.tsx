import Link from "next/link"
import { ChevronRight } from "lucide-react"

interface BreadcrumbItem {
  label: string
  href?: string
}

interface BreadcrumbsTPUProps {
  items: BreadcrumbItem[]
}

export default function BreadcrumbsTPU({ items }: BreadcrumbsTPUProps) {
  return (
    <nav aria-label="Breadcrumb" className="mb-6">
      <ol className="flex flex-wrap items-center gap-1.5 text-sm">
        <li>
          <Link
            href="/"
            className="text-[var(--color-tpu-text-secondary)] hover:text-[var(--color-tpu-accent)] transition-colors"
          >
            Главная
          </Link>
        </li>
        {items.map((item, index) => (
          <li key={index} className="flex items-center gap-1.5">
            <ChevronRight
              size={14}
              className="text-[var(--color-tpu-text-secondary)]"
            />
            {item.href ? (
              <Link
                href={item.href}
                className="text-[var(--color-tpu-text-secondary)] hover:text-[var(--color-tpu-accent)] transition-colors"
              >
                {item.label}
              </Link>
            ) : (
              <span className="text-[var(--color-tpu-text-primary)] font-medium">
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  )
}
