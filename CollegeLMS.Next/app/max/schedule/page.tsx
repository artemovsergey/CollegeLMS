import dynamic from "next/dynamic"

const ScheduleView = dynamic(() => import("@/components/max/ScheduleView"), {
  ssr: false,
})

export default function MaxSchedulePage() {
  return <ScheduleView />
}