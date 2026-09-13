import dynamic from "next/dynamic"

const FavoritesView = dynamic(
  () => import("@/components/max/FavoritesView"),
  { ssr: false },
)

export const metadata = {
  title: "Избранное",
}

export default function MaxFavoritesPage() {
  return <FavoritesView />
}