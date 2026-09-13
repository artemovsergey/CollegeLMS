import dynamic from "next/dynamic"

const ChangesView = dynamic(() => import("@/components/max/ChangesView"), {
  ssr: false,
})

export const metadata = {
  title: "Изменения",
}

export default function MaxChangesPage() {
  return <ChangesView />
}