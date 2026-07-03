#!/usr/bin/env pwsh

$logFile = Join-Path (Get-Location) "agent-timeline.jsonl"

if (-not (Test-Path -LiteralPath $logFile)) {
    Write-Output "No agent-timeline.jsonl found. Start a session first."
    exit 0
}

$lines = Get-Content -Path $logFile -Encoding UTF8 | Where-Object { $_ -ne "" }

if ($lines.Count -eq 0) {
    Write-Output "agent-timeline.jsonl is empty."
    exit 0
}

$events = $lines | ForEach-Object { $_ | ConvertFrom-Json }

# — Group events into tasks (between task_start and task_end) —
$tasks = @()
$currentTask = $null

foreach ($ev in $events) {
    switch ($ev.event) {
        "session_start" {
            # no-op, just a marker
        }
        "task_start" {
            if ($currentTask -ne $null) {
                Write-Warning "task_start #$($ev.id) while task #$($currentTask.id) still open; closing previous"
                $currentTask.ts_end = $ev.ts
                $tasks += $currentTask
            }
            $currentTask = @{
                id    = $ev.id
                name  = $ev.name
                ts_start = $ev.ts
                ts_end   = $null
                toolCalls = @()  # @{ tool, ts_call, ts_result }
                openTool  = $null
            }
        }
        "tool_call" {
            if ($currentTask -ne $null) {
                $currentTask.openTool = @{
                    tool = $ev.tool
                    ts_call = $ev.ts
                }
            }
        }
        "tool_result" {
            if ($currentTask -ne $null -and $currentTask.openTool -ne $null) {
                $currentTask.openTool.ts_result = $ev.ts
                $currentTask.toolCalls += $currentTask.openTool
                $currentTask.openTool = $null
            }
        }
        "task_end" {
            if ($currentTask -ne $null) {
                if ($currentTask.id -ne $ev.id) {
                    Write-Warning "task_end id=$($ev.id) doesn't match current task id=$($currentTask.id)"
                }
                $currentTask.ts_end = $ev.ts
                $tasks += $currentTask
                $currentTask = $null
            }
        }
        "session_end" {
            # no-op
        }
    }
}

# Close dangling task (if session ended without task_end)
if ($currentTask -ne $null) {
    Write-Warning "Unclosed task #$($currentTask.id) — using last event timestamp"
    $currentTask.ts_end = ($events | Select-Object -Last 1).ts
    $tasks += $currentTask
}

if ($tasks.Count -eq 0) {
    Write-Output "No tasks found in timeline."
    exit 0
}

# — Calculate and display —
function Format-Duration($ms) {
    if ($ms -lt 1000) { return "$($ms)ms" }
    $s = [Math]::Round($ms / 1000, 1)
    if ($s -lt 60) { return "${s}s" }
    $m = [Math]::Floor($s / 60)
    $sec = $s % 60
    return "${m}m ${sec}s"
}

$header = "`nTask Timeline Report"
$header += "`n" + ("=" * $header.Length)
$header

$totalWall = 0
$totalThink = 0
$totalTool = 0

$rows = @()

foreach ($t in $tasks) {
    $wall = $t.ts_end - $t.ts_start
    $toolMs = 0
    $toolSummary = @{}

    foreach ($tc in $t.toolCalls) {
        if ($tc.ts_result -ne $null) {
            $dur = $tc.ts_result - $tc.ts_call
            $toolMs += $dur
            if (-not $toolSummary.ContainsKey($tc.tool)) {
                $toolSummary[$tc.tool] = 0
            }
            $toolSummary[$tc.tool] += $dur
        }
    }

    $think = [Math]::Max(0, $wall - $toolMs)

    $toolParts = $toolSummary.GetEnumerator() | Sort-Object Name | ForEach-Object {
        "$($_.Name)( $(Format-Duration $_.Value) )"
    }
    $toolStr = if ($toolParts.Count -gt 0) { $toolParts -join ", " } else { "-" }

    $rows += [PSCustomObject]@{
        ID      = $t.id
        Name    = $t.name
        Wall    = $wall
        Think   = $think
        Tool    = $toolMs
        ToolStr = $toolStr
    }

    $totalWall += $wall
    $totalThink += $think
    $totalTool += $toolMs
}

# Determine column widths
$maxNameLen = ($rows | ForEach-Object { $_.Name.Length }) | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
$nameCol = [Math]::Min([Math]::Max($maxNameLen, 20), 60)

$fmt = " {0,-3} | {1,-$nameCol} | {2,-8} | {3,-8} | {4,-8} | {5,-30}"

$fmt -f "#", "Task", "Wall", "Think", "Waiting", "Tools"
"-" * ($fmt -f "#", "Task", "Wall", "Think", "Waiting", "Tools").Length

foreach ($r in $rows) {
    $fmt -f $r.ID, $r.Name, (Format-Duration $r.Wall), (Format-Duration $r.Think), (Format-Duration $r.Tool), $r.ToolStr
}

"-" * ($fmt -f "#", "Task", "Wall", "Think", "Waiting", "Tools").Length
$fmt -f "", "Total", (Format-Duration $totalWall), (Format-Duration $totalThink), (Format-Duration $totalTool), ""

Write-Output ""
