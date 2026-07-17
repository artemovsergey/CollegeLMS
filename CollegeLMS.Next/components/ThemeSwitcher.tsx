"use client"

import { useState } from "react"
import { Palette, X } from "lucide-react"
import { useThemePreset, type ThemePreset } from "@/lib/theme-preset"
import { useTheme } from "next-themes"

const SWATCHES: Record<ThemePreset, string[]> = {
  blue: ["#568edd", "#d5e3f7"],
  indigo: ["#24386a", "#c9ceda"],
  sapphire: ["#4d74b4", "#d3dded"],
  plum: ["#b9b3e5", "#eeecf9"],
  green: ["#2f8733", "#cbe1cc"],
}

export default function ThemeSwitcher() {
  const [open, setOpen] = useState(false)
  const { preset, setPreset, allPresets } = useThemePreset()
  const { theme, setTheme } = useTheme()

  return (
    <>
      <button
        onClick={() => setOpen(!open)}
        className="fixed bottom-6 right-6 z-50 flex h-12 w-12 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg transition-all hover:scale-105 hover:shadow-xl"
        aria-label="Цветовая схема"
      >
        {open ? <X size={20} /> : <Palette size={20} />}
      </button>

      {open && (
        <div className="fixed bottom-20 right-6 z-50 w-56 rounded-xl border border-border bg-card p-4 shadow-2xl">
          <p className="mb-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Цветовая схема
          </p>
          <div className="mb-3 flex flex-col gap-1.5">
            {allPresets.map((p) => (
              <button
                key={p.value}
                onClick={() => setPreset(p.value)}
                className={`flex items-center gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                  preset === p.value
                    ? "bg-accent/10 ring-1 ring-accent"
                    : "hover:bg-muted"
                }`}
              >
                <span className="flex shrink-0 gap-0.5">
                  {SWATCHES[p.value].map((color, i) => (
                    <span
                      key={i}
                      className="block h-5 w-4 rounded-sm"
                      style={{ backgroundColor: color }}
                    />
                  ))}
                </span>
                <span className="flex flex-col">
                  <span className="font-medium text-foreground">{p.label}</span>
                  <span className="text-xs text-muted-foreground">
                    {p.description}
                  </span>
                </span>
              </button>
            ))}
          </div>
          <div className="border-t border-border pt-3">
            <p className="mb-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Режим
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => setTheme("light")}
                className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
                  theme === "light"
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                Светлая
              </button>
              <button
                onClick={() => setTheme("dark")}
                className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium transition-colors ${
                  theme === "dark"
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                Тёмная
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
