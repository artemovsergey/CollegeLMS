"use client"

import { useState, useEffect } from "react"
import { Cookie, X } from "lucide-react"

export default function CookieConsent() {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const consent = localStorage.getItem("cookie-consent")
    if (!consent) {
      setVisible(true)
    }
  }, [])

  const accept = () => {
    localStorage.setItem("cookie-consent", "accepted")
    setVisible(false)
  }

  if (!visible) return null

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 border-t border-border bg-card p-4 shadow-lg">
      <div className="mx-auto flex max-w-7xl flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <Cookie size={20} className="mt-0.5 shrink-0 text-primary" />
          <p className="text-sm text-muted-foreground">
            Мы используем файлы cookie для улучшения работы сайта. Продолжая
            пользоваться сайтом, вы соглашаетесь с использованием cookie.
          </p>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <button
            onClick={accept}
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Принять
          </button>
          <button
            onClick={accept}
            className="rounded-md p-2 text-muted-foreground transition-colors hover:bg-muted"
            aria-label="Закрыть"
          >
            <X size={18} />
          </button>
        </div>
      </div>
    </div>
  )
}
