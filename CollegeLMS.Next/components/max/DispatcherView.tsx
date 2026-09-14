"use client"

import { useState } from "react"
import { LogOut } from "lucide-react"
import { Button, MaxUI, Typography } from "@maxhub/max-ui"
import {
  dispatcherToken,
  dispatcherLogout,
  notifyDispatcherSession,
} from "@/api/dispatcher"
import type { ConfirmResult } from "@/types/correction"
import { useMaxContext } from "@/lib/max-context"
import DispatcherGate from "@/components/max/DispatcherGate"
import DispatcherImport from "@/components/max/DispatcherImport"
import DispatcherManual from "@/components/max/DispatcherManual"
import DispatcherResult from "@/components/max/DispatcherResult"

export default function DispatcherView() {
  const { viewContext } = useMaxContext()
  const [token, setToken] = useState<string | null>(() => dispatcherToken())
  const [mode, setMode] = useState<"file" | "manual">("file")
  const [applied, setApplied] = useState<ConfirmResult | null>(null)
  const [resultKey, setResultKey] = useState(0)

  const onApplied = (result: ConfirmResult) => {
    setApplied(result)
    setResultKey((k) => k + 1)
  }

  const logout = () => {
    dispatcherLogout()
    notifyDispatcherSession()
    setApplied(null)
    setToken(null)
  }

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <Typography.Title>Диспетчер</Typography.Title>
          {token ? (
            <Button
              size="small"
              variant="ghost"
              aria-label="Выйти из режима диспетчера"
              onClick={logout}
              iconBefore={<LogOut size={16} aria-hidden />}
            />
          ) : null}
        </header>

        {token ? (
          <>
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
                groupId={viewContext.groupId}
              />
            ) : null}
          </>
        ) : (
          <DispatcherGate onSuccess={() => setToken(dispatcherToken())} />
        )}
      </main>
    </MaxUI>
  )
}