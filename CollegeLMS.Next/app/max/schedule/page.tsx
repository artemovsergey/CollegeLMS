import dynamic from "next/dynamic"

const ScheduleView = dynamic(() => import("@/components/max/ScheduleView"), {
  ssr: false,
})

export const metadata = {
  title: "Расписание",
}

export default function MaxSchedulePage() {
  return <ScheduleView />
}