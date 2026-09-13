import type { Metadata } from "next"
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
      <MaxShell>{children}</MaxShell>
    </MaxContextProvider>
  )
}