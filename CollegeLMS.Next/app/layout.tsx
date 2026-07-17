import type { Metadata } from "next"
import "./globals.css"
import { AuthProvider } from "@/lib/auth"
import { ThemeProvider } from "next-themes"
import { ThemePresetProvider } from "@/lib/theme-preset"
import { Toaster } from "@/components/ui/sonner"
import ThemeSwitcher from "@/components/ThemeSwitcher"
import "@fontsource/golos-text"
import "@fontsource/golos-text/500.css"
import "@fontsource/golos-text/600.css"
import "@fontsource/golos-text/700.css"

export const metadata: Metadata = {
  title: {
    default: "ГБПОУ СКС — Ставропольский колледж связи",
    template: "%s — ГБПОУ СКС",
  },
  description:
    "ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»",
  icons: {
    icon: "/favicon.ico",
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
      <body className="min-h-screen bg-background text-foreground antialiased">
        <ThemeProvider
          attribute="class"
          defaultTheme="light"
          enableSystem={false}
          storageKey="theme"
        >
          <ThemePresetProvider>
            <AuthProvider>
              {children}
              <ThemeSwitcher />
              <Toaster />
            </AuthProvider>
          </ThemePresetProvider>
        </ThemeProvider>
      </body>
    </html>
  )
}
