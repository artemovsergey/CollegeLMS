"use client"

import { Settings } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

export default function DispatcherCorrectionPage() {
  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Корректировка расписания</h2>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Settings size={16} /> Раздел в разработке
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Здесь будет возможность корректировать расписание: замены преподавателей, перенос занятий, изменение аудиторий.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
