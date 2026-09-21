import type { Metadata } from "next"
import Script from "next/script"
import { MaxContextProvider } from "@/lib/max-context"
import MaxShell from "@/components/max/MaxShell"
import "./max.css"

export const metadata: Metadata = {
  title: {
    default: "Расписание",
    template: "%s — Расписание",
  },
}

export default function MaxLayout({ children }: { children: React.ReactNode }) {
  return (
    <MaxContextProvider>
      {/* MAX Bridge: даёт window.WebApp (initDataUnsafe.start_param) */}
      <Script src="https://st.max.ru/js/max-web-app.js" strategy="afterInteractive" />
      <MaxShell>{children}</MaxShell>
    </MaxContextProvider>
  )
}