"use client"

import { createContext, useContext, useEffect, useState } from "react"

export type DesignPreset = "default" | "tpu"

const DESIGN_PRESETS: DesignPreset[] = ["default", "tpu"]

const DESIGN_LABELS: Record<DesignPreset, string> = {
  default: "Стандартный",
  tpu: "ТПУ",
}

const DEFAULT_DESIGN: DesignPreset = "default"

const DesignContext = createContext<{
  design: DesignPreset
  setDesign: (d: DesignPreset) => void
  allDesigns: { value: DesignPreset; label: string }[]
} | null>(null)

export function DesignProvider({ children }: { children: React.ReactNode }) {
  const [design, setDesign] = useState<DesignPreset>(DEFAULT_DESIGN)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    const stored = localStorage.getItem("design-preset") as DesignPreset | null
    if (stored && DESIGN_PRESETS.includes(stored)) {
      setDesign(stored)
    }
    setMounted(true)
  }, [])

  useEffect(() => {
    if (!mounted) return
    document.documentElement.setAttribute("data-design", design)
    localStorage.setItem("design-preset", design)
  }, [design, mounted])

  const allDesigns = DESIGN_PRESETS.map((value) => ({
    value,
    label: DESIGN_LABELS[value],
  }))

  return (
    <DesignContext.Provider value={{ design, setDesign, allDesigns }}>
      {children}
    </DesignContext.Provider>
  )
}

export function useDesign() {
  const ctx = useContext(DesignContext)
  if (!ctx) return { design: "default" as DesignPreset, setDesign: () => {}, allDesigns: [{ value: "default" as DesignPreset, label: "Стандартный" }] }
  return ctx
}
