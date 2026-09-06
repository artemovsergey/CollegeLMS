"use client"

import { useEffect } from "react"

export function ScrollBarFix() {
  useEffect(() => {
    const getScrollbarWidth = () =>
      window.innerWidth - document.documentElement.clientWidth

    let lastScrollbarWidth = getScrollbarWidth()

    const observer = new MutationObserver(() => {
      const currentScrollbarWidth = getScrollbarWidth()

      if (document.body.style.overflow === "hidden" && lastScrollbarWidth > 0) {
        document.body.style.paddingRight = `${lastScrollbarWidth}px`
      } else if (
        document.body.style.overflow !== "hidden" &&
        document.body.style.paddingRight
      ) {
        document.body.style.paddingRight = ""
        lastScrollbarWidth = getScrollbarWidth()
      } else {
        lastScrollbarWidth = currentScrollbarWidth
      }
    })

    observer.observe(document.body, {
      attributes: true,
      attributeFilter: ["style"],
    })

    return () => observer.disconnect()
  }, [])

  return null
}
