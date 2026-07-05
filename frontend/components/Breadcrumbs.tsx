import Link from "next/link"

interface BreadcrumbItem {
  label: string
  href?: string
}

export default function Breadcrumbs({ items }: { items: BreadcrumbItem[] }) {
  return (
    <nav className="mb-6 text-sm text-[#5a6a8a]" aria-label="Breadcrumb">
      <ol className="flex flex-wrap items-center gap-1">
        <li>
          <Link href="/" className="hover:text-[#568cd6] transition-colors">
            Главная
          </Link>
        </li>
        {items.map((item, i) => (
          <li key={i} className="flex items-center gap-1">
            <span className="mx-1">/</span>
            {item.href ? (
              <Link href={item.href} className="hover:text-[#568cd6] transition-colors">
                {item.label}
              </Link>
            ) : (
              <span className="text-[#152851] font-medium">{item.label}</span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  )
}
