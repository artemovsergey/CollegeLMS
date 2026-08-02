"use client"

import { useEffect, useState } from "react"
import { AlertTriangle } from "lucide-react"
import api from "@/lib/api"

const CHECK_INTERVAL_MS = 60_000

// ВРЕМЕННО: плашка только на время разработки.
// Убрать вместе с эндпоинтом /api/health/stvcc после перехода с stvcc.ru.
export default function StvccUnavailableBanner() {
  const [available, setAvailable] = useState<boolean | null>(null)

  useEffect(() => {
    let cancelled = false

    async function check() {
      try {
        const res = await api.get("/api/health/stvcc")
        if (!cancelled) setAvailable(res.data?.data?.available === true)
      } catch {
        if (!cancelled) setAvailable(null)
      }
    }

    check()
    const timer = setInterval(check, CHECK_INTERVAL_MS)
    return () => {
      cancelled = true
      clearInterval(timer)
    }
  }, [])

  if (available !== false) return null

  return (
    <div
      role="status"
      className="flex items-center justify-center gap-2 bg-amber-400 px-4 py-1.5 text-center text-xs font-medium text-amber-950 sm:text-sm"
    >
      <AlertTriangle size={16} className="shrink-0" aria-hidden="true" />
      Источник данных (stvcc.ru) временно недоступен — часть контента может не загружаться
    </div>
  )
}
