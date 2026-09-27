import type { Metadata } from "next"
import "./globals.css"
import { AuthProvider } from "@/lib/auth"
import { ThemeProvider } from "next-themes"
import { Toaster } from "@/components/ui/sonner"
import { TooltipProvider } from "@/components/ui/tooltip"
import "@fontsource/inter"
import "@fontsource/inter/500.css"
import "@fontsource/inter/600.css"
import "@fontsource/inter/700.css"
import "@maxhub/max-ui/dist/styles.css"

// Класс режима высокой контрастности применяется до первой отрисовки,
// иначе при включённом режиме страница успевает отрисоваться в обычных
// цветах и переключается уже после гидратации (мигание).
const accessibilityModeScript = `(function(){try{if(localStorage.getItem("accessibility-mode")==="true"){document.documentElement.classList.add("accessibility-mode")}}catch(e){}})();`

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
        <script dangerouslySetInnerHTML={{ __html: accessibilityModeScript }} />
        <ThemeProvider
          attribute="class"
          defaultTheme="light"
          enableSystem={false}
          storageKey="theme"
        >
          <AuthProvider>
            <TooltipProvider delayDuration={300}>
              {children}
              <Toaster />
            </TooltipProvider>
          </AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  )
}
