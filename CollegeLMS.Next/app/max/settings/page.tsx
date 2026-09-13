import dynamic from "next/dynamic"

const SettingsView = dynamic(() => import("@/components/max/SettingsView"), {
  ssr: false,
})

export const metadata = {
  title: "Уведомления",
}

export default function MaxSettingsPage() {
  return <SettingsView />
}