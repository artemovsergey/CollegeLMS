"use client"

import Image from "next/image"
import { useState, useEffect, type ReactNode } from "react"

interface AnimatedLogoLoaderProps {
  children: ReactNode
  minDuration?: number
}

export default function AnimatedLogoLoader({
  children,
  minDuration = 2000,
}: AnimatedLogoLoaderProps) {
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const timer = setTimeout(() => setLoading(false), minDuration)
    return () => clearTimeout(timer)
  }, [minDuration])

  if (!loading) return <>{children}</>

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-bg">
      <div className="animate-logo-pulse">
        <Image
          src="/logo.svg"
          alt="Загрузка..."
          width={0}
          height={0}
          sizes="100vw"
          className="h-40 w-auto object-contain"
          style={{ width: 'auto', height: '100%' }}
          unoptimized
        />
      </div>
    </div>
  )
}
