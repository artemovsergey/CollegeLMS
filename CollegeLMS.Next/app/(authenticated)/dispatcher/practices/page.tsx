"use client"

import { Briefcase } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

export default function DispatcherPracticesPage() {
  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Учебные практики</h2>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Briefcase size={16} /> Раздел в разработке
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Управление учебными практиками: УП, ПП, экзамены. Назначение преподавателей, формирование групп, отслеживание прохождения.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
