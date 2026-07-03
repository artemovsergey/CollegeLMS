#!/usr/bin/env pwsh
param(
    [Parameter(Position=0, Mandatory=$true)]
    [string]$Event,
    [Parameter(Position=1)]
    [string]$Arg1,
    [Parameter(Position=2)]
    [string]$Arg2
)

$ErrorActionPreference = "Stop"
$logFile = Join-Path (Get-Location) "agent-timeline.jsonl"
$ts = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

function Escape-Json($s) {
    if ($s -eq $null) { return "" }
    return $s.Replace("\", "\\").Replace('"', '\"').Replace("`n", "\n").Replace("`r", "\r").Replace("`t", "\t")
}

switch ($Event) {
    "session_start" {
        $entry = "{`"event`":`"session_start`",`"ts`":$ts}"
    }
    "session_end" {
        $entry = "{`"event`":`"session_end`",`"ts`":$ts}"
    }
    "task_start" {
        $escapedName = Escape-Json($Arg2)
        $entry = "{`"event`":`"task_start`",`"id`":`"$(Escape-Json($Arg1))`",`"name`":`"$escapedName`",`"ts`":$ts}"
    }
    "task_end" {
        $entry = "{`"event`":`"task_end`",`"id`":`"$(Escape-Json($Arg1))`",`"ts`":$ts}"
    }
    "tool_call" {
        $entry = "{`"event`":`"tool_call`",`"tool`":`"$(Escape-Json($Arg1))`",`"ts`":$ts}"
    }
    "tool_result" {
        $entry = "{`"event`":`"tool_result`",`"tool`":`"$(Escape-Json($Arg1))`",`"ts`":$ts}"
    }
    default {
        Write-Error "Unknown event: $Event. Valid: session_start, session_end, task_start, task_end, tool_call, tool_result"
        exit 1
    }
}

Add-Content -Path $logFile -Value $entry -Encoding UTF8
Write-Output "[timeline] $entry"
