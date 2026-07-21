"use client"

import { useState, useEffect } from "react"
import { Cookie } from "lucide-react"

const STORAGE_KEY = "cookie-consent"

export default function CookieConsent() {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const consent = localStorage.getItem(STORAGE_KEY)
    if (!consent) setVisible(true)
  }, [])

  function accept() {
    localStorage.setItem(STORAGE_KEY, "accepted")
    setVisible(false)
  }

  if (!visible) return null

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 border-t border-border bg-card p-4 shadow-lg">
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
        <div className="flex items-start gap-3 text-sm text-muted-fg">
          <Cookie size={20} className="shrink-0 mt-0.5 text-accent" />
          <p>
            Продолжая использовать сайт, вы соглашаетесь на обработку файлов cookie
            и политику конфиденциальности.
          </p>
        </div>
        <button
          onClick={accept}
          className="shrink-0 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-accent-hover"
        >
          Принять
        </button>
      </div>
    </div>
  )
}
