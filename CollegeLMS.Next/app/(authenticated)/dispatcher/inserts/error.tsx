"use client"

import ReferenceErrorState from "@/components/ReferenceErrorState"

export default function InsertsError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return <ReferenceErrorState error={error} reset={reset} title="События" />
}
