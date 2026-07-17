"use client"

import { createContext, useContext, useEffect, useState } from "react"

export type ThemePreset = "navy-blue" | "navy-green" | "navy-gold" | "blue-dark"

const THEME_PRESETS: ThemePreset[] = ["navy-blue", "navy-green", "navy-gold", "blue-dark"]

const THEME_LABELS: Record<ThemePreset, string> = {
  "navy-blue": "Синий",
  "navy-green": "Зелёный",
  "navy-gold": "Золотой",
  "blue-dark": "Тёмный",
}

const THEME_DESCRIPTIONS: Record<ThemePreset, string> = {
  "navy-blue": "Классический",
  "navy-green": "Свежий",
  "navy-gold": "Тёплый",
  "blue-dark": "Деловой",
}

const DEFAULT_PRESET: ThemePreset = "navy-blue"

const ThemePresetContext = createContext<{
  preset: ThemePreset
  setPreset: (preset: ThemePreset) => void
  allPresets: { value: ThemePreset; label: string; description: string }[]
} | null>(null)

export function ThemePresetProvider({ children }: { children: React.ReactNode }) {
  const [preset, setPreset] = useState<ThemePreset>(DEFAULT_PRESET)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    const stored = localStorage.getItem("theme-preset") as ThemePreset | null
    if (stored && THEME_PRESETS.includes(stored)) {
      setPreset(stored)
    }
    setMounted(true)
  }, [])

  useEffect(() => {
    if (!mounted) return
    document.documentElement.setAttribute("data-theme", preset)
    localStorage.setItem("theme-preset", preset)
  }, [preset, mounted])

  const allPresets = THEME_PRESETS.map((value) => ({
    value,
    label: THEME_LABELS[value],
    description: THEME_DESCRIPTIONS[value],
  }))

  return (
    <ThemePresetContext.Provider value={{ preset, setPreset, allPresets }}>
      {children}
    </ThemePresetContext.Provider>
  )
}

export function useThemePreset() {
  const ctx = useContext(ThemePresetContext)
  if (!ctx) throw new Error("useThemePreset must be used within ThemePresetProvider")
  return ctx
}
