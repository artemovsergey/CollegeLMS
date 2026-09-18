"use client"

import { useState } from "react"
import { MaxUI, Typography } from "@maxhub/max-ui"
import type { CorrectionApplyResult } from "@/types/correction"
import DispatcherImport from "@/components/max/DispatcherImport"
import DispatcherManual from "@/components/max/DispatcherManual"
import DispatcherResult from "@/components/max/DispatcherResult"

export default function DispatcherView() {
  const [mode, setMode] = useState<"file" | "manual">("file")
  const [applied, setApplied] = useState<CorrectionApplyResult | null>(null)
  const [resultKey, setResultKey] = useState(0)

  const onApplied = (result: CorrectionApplyResult) => {
    setApplied(result)
    setResultKey((k) => k + 1)
  }

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <Typography.Title>Диспетчер</Typography.Title>
        </header>

        <div
          className="max-schedule__view-switch"
          role="tablist"
          aria-label="Способ корректировки"
        >
          <button
            type="button"
            role="tab"
            aria-selected={mode === "file"}
            className={`max-schedule__view-tab ${
              mode === "file" ? "max-schedule__view-tab--active" : ""
            }`}
            onClick={() => setMode("file")}
          >
            Файл XLSX
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={mode === "manual"}
            className={`max-schedule__view-tab ${
              mode === "manual" ? "max-schedule__view-tab--active" : ""
            }`}
            onClick={() => setMode("manual")}
          >
            Вручную
          </button>
        </div>

        {mode === "file" ? (
          <DispatcherImport onApplied={onApplied} />
        ) : (
          <DispatcherManual onApplied={onApplied} />
        )}
        {applied ? (
          <DispatcherResult
            key={resultKey}
            applied={applied}
          />
        ) : null}
      </main>
    </MaxUI>
  )
}
