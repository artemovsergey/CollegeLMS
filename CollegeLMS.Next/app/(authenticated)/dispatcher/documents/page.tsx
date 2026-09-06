"use client"

import { FileText } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

export default function DispatcherDocumentsPage() {
  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Документы</h2>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <FileText size={16} /> Раздел в разработке
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Здесь будут формироваться и скачиваться учебные документы: журналы посещений, ведомости, отчёты.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
