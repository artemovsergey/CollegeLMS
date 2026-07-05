import type { Metadata } from "next"
import "./globals.css"
import { AuthProvider } from "@/lib/auth"
import { ThemeProvider } from "next-themes"
import CookieConsent from "@/components/CookieConsent"

export const metadata: Metadata = {
  title: {
    default: "Ставропольский колледж связи им. В. А. Петрова",
    template: "%s — СКС им. В. А. Петрова",
  },
  description:
    "Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова — государственное бюджетное профессиональное образовательное учреждение",
  icons: {
    icon: "/favicon.ico",
    apple: "/apple-touch-icon.png",
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
          <AuthProvider>{children}</AuthProvider>
          <CookieConsent />
        </ThemeProvider>
      </body>
    </html>
  )
}
