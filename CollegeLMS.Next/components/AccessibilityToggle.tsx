"use client"

import { useEffect, useState } from "react"
import { Eye } from "lucide-react"
import { Button } from "@/components/ui/button"

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
    <Button
      variant="ghost"
      size="icon"
      onClick={toggle}
      className={isActive ? "bg-muted text-accent" : undefined}
      aria-label="Версия для слабовидящих"
      aria-pressed={isActive}
    >
      <Eye size={18} />
    </Button>
  )
}
