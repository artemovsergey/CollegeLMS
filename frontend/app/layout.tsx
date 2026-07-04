import type { Metadata } from "next"
import "./globals.css"
import { AuthProvider } from "@/lib/auth"
import { ThemeProvider } from "next-themes"

export const metadata: Metadata = {
  title: {
    default: "ГБПОУ СКС — Ставропольский колледж связи",
    template: "%s — ГБПОУ СКС",
  },
  description:
    "ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»",
  icons: {
    icon: "/logo.png",
    apple: "/logo.png",
  },
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="ru" suppressHydrationWarning>
      <body className="min-h-screen bg-[#f5f7fa] text-[#152851] antialiased">
        <ThemeProvider
          attribute="class"
          defaultTheme="light"
          enableSystem={false}
          storageKey="theme"
        >
          <AuthProvider>{children}</AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  )
}
