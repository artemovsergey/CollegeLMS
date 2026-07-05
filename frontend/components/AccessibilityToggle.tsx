"use client"

import { useEffect, useState } from "react"
import { Eye } from "lucide-react"

export default function AccessibilityToggle() {
  const [isActive, setIsActive] = useState(false)

  useEffect(() => {
    const saved = localStorage.getItem("accessibility-mode") === "true"
    setIsActive(saved)
    if (saved) {
      document.documentElement.classList.add("accessibility-mode")
    } else {
      document.documentElement.classList.remove("accessibility-mode")
    }
  }, [])

  const toggle = () => {
    const next = !isActive
    setIsActive(next)
    localStorage.setItem("accessibility-mode", String(next))
    if (next) {
      document.documentElement.classList.add("accessibility-mode")
    } else {
      document.documentElement.classList.remove("accessibility-mode")
    }
  }

  return (
    <button
      onClick={toggle}
      className={`rounded-md p-2 transition-colors hover:bg-muted ${
        isActive ? "bg-muted text-primary" : "text-muted-foreground"
      }`}
      aria-label="Версия для слабовидящих"
    >
      <Eye size={18} />
    </button>
  )
}
