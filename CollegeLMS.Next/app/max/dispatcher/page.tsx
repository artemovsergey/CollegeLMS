import dynamic from "next/dynamic"

const DispatcherView = dynamic(() => import("@/components/max/DispatcherView"), {
  ssr: false,
})

export const metadata = {
  title: "Диспетчер",
}

export default function MaxDispatcherPage() {
  return <DispatcherView />
}