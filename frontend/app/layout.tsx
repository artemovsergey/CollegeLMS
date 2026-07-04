import type { Metadata } from "next"
import "./globals.css"
import { AuthProvider } from "@/lib/auth"

export const metadata: Metadata = {
  title: {
    default: "СКСС — Ставропольский колледж связи",
    template: "%s — СКСС",
  },
  description: "ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»",
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="ru">
      <body className="min-h-screen bg-[#f5f7fa] text-[#152851] antialiased">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  )
}
