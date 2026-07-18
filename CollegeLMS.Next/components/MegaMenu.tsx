"use client"

import Link from "next/link"
import type { Section } from "@/data/site-content"

export default function MegaMenu({ section }: { section: Section }) {
  return (
    <div className="absolute left-0 top-full mt-0 w-screen max-w-4xl rounded-b-lg border border-t-0 border-gray-200 bg-white shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 z-50">
      <div className="grid grid-cols-3 gap-6 p-6">
        {section.subsections.map((sub) => (
          <Link
            key={sub.slug}
            href={sub.href}
            className="text-sm text-gray-700 hover:text-[#0066cc] transition-colors"
          >
            {sub.title}
          </Link>
        ))}
      </div>
    </div>
  )
}
