import dynamic from "next/dynamic"

const MaxScheduleView = dynamic(() => import("@/components/MaxScheduleView"), {
  ssr: false,
})

export const metadata = {
  title: "Расписание",
}

export default function MaxSchedulePage() {
  return <MaxScheduleView />
}
