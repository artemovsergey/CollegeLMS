"use client"

import { useState, useEffect, type ReactNode } from "react"

interface AnimatedLogoLoaderProps {
  children: ReactNode
  minDuration?: number
}

export default function AnimatedLogoLoader({
  children,
  minDuration = 800,
}: AnimatedLogoLoaderProps) {
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const timer = setTimeout(() => setLoading(false), minDuration)
    return () => clearTimeout(timer)
  }, [minDuration])

  if (!loading) return <>{children}</>

  return (
    <div className="fixed inset-0 z-50 flex flex-col items-center justify-center gap-6 bg-bg">
      <div className="h-20 w-60 animate-pulse rounded-lg bg-muted" />
      <div className="h-4 w-40 animate-pulse rounded bg-muted" />
      <div className="mt-4 h-3 w-80 animate-pulse rounded bg-muted" />
      <div className="h-3 w-72 animate-pulse rounded bg-muted" />
      <div className="h-3 w-76 animate-pulse rounded bg-muted" />
    </div>
  )
}
