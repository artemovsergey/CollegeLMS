import LoadingSpinner from "@/components/LoadingSpinner"

export default function ChangesLoading() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center p-6">
      <LoadingSpinner size="lg" />
    </div>
  )
}
