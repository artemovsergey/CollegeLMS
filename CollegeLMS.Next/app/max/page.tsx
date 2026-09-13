import dynamic from "next/dynamic"

const HomeView = dynamic(() => import("@/components/max/HomeView"), {
  ssr: false,
})

export const metadata = {
  title: "Главная",
}

export default function MaxHomePage() {
  return <HomeView />
}