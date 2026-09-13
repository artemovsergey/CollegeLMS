import dynamic from "next/dynamic"

const JournalView = dynamic(() => import("@/components/max/JournalView"), {
  ssr: false,
})

export const metadata = {
  title: "Журнал",
}

export default function MaxJournalPage() {
  return <JournalView />
}