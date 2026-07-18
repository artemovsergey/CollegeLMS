import type { ReactNode } from "react"
import Header from "@/components/Header"
import Footer from "@/components/Footer"
import { DesignProvider } from "@/lib/design-provider"

export default function PublicLayout({ children }: { children: ReactNode }) {
  return (
    <DesignProvider>
      <div className="flex min-h-screen flex-col">
        <Header />
        <main className="flex-1">{children}</main>
        <Footer />
      </div>
    </DesignProvider>
  )
}
