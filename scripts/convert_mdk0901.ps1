# Скрипт конвертации репозитория mdk0901 в import/mdk0901_course.json
# Порядок: разделы 1-9; внутри раздела: лекции, практики, самостоятельные (по номеру)
# HTML-блоки вне fenced code оборачиваются в ```html

param(
    [string]$RepoPath = "$env:TEMP\opencode\mdk0901",
    [string]$ReactPath = "C:\Users\asv\Desktop\CollegeLMS\import\mdk0901_react",
    [string]$OutPath = "C:\Users\asv\Desktop\CollegeLMS\import\mdk0901_course.json"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Get-LessonType([string]$name) {
    if ($name -match '^Лекция\s') { return 'Lecture' }
    if ($name -match '^Практическая работа\s') { return 'Practice' }
    if ($name -match '^Самостоятельная работа\s') { return 'SelfStudy' }
    throw "Неизвестный тип занятия: $name"
}

function Get-LessonNumber([string]$name) {
    if ($name -match '^[^0-9]+?\s+(\d+)') { return [int]$Matches[1] }
    return 9999
}

function Convert-Content([string]$content) {
    $content = $content -replace "`r`n", "`n" -replace "`r", ""
    $lines = $content -split "`n"
    $sb = New-Object System.Text.StringBuilder
    $inFence = $false
    $htmlBlock = New-Object System.Collections.Generic.List[string]

    function Flush-Html($block) {
        if ($block.Count -eq 0) { return }
        $null = $sb.AppendLine('```html')
        foreach ($l in $block) { $null = $sb.AppendLine($l) }
        $null = $sb.AppendLine('```')
        $block.Clear()
    }

    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^```') {
            Flush-Html $htmlBlock
            $inFence = -not $inFence
            $null = $sb.AppendLine($line)
            continue
        }
        if (-not $inFence -and $trimmed -match '^<[a-zA-Z!][^>]*>$' -and $trimmed -notmatch '^<\!') {
            $htmlBlock.Add($line)
            continue
        }
        Flush-Html $htmlBlock
        $null = $sb.AppendLine($line)
    }
    Flush-Html $htmlBlock
    return $sb.ToString().Trim("`n")
}

$sections = @()
foreach ($dir in (Get-ChildItem "$RepoPath\course" -Directory | Sort-Object { $_.Name })) {
    if ($dir.Name -match 'Раздел 5|Раздел 7') { continue }
    $sections += , @{
        Files  = (Get-ChildItem $dir.FullName -Filter *.md)
        Name   = $dir.Name
        IsRepo = $true
    }
}
foreach ($dir in (Get-ChildItem $ReactPath -Directory | Sort-Object { $_.Name })) {
    $sections += , @{
        Files  = (Get-ChildItem $dir.FullName -Filter *.md)
        Name   = $dir.Name
        IsRepo = $false
    }
}

$ordered = @()
$orderByNumber = {
    if ($args[0] -match '^Раздел (\d+)') { [int]$Matches[1] } else { 99 }
}
$ordered = $sections | Sort-Object { if ($_.Name -match '^Раздел (\d+)') { [int]$Matches[1] } else { 99 } }

$lectures = New-Object System.Collections.Generic.List[object]
$order = 0

foreach ($section in $ordered) {
    $sorted = $section.Files | Sort-Object @{ Expression = { Get-LessonType $_.Name } }, @{ Expression = { Get-LessonNumber $_.Name } }
    foreach ($file in $sorted) {
        $order++
        $title = [System.IO.Path]::GetFileNameWithoutExtension($file.Name) -replace '\s+', ' '
        $content = Convert-Content ([System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8))
        $lectures.Add([pscustomobject]@{
            title       = $title.Trim()
            lectureType = Get-LessonType $file.Name
            order       = $order
            content     = $content
        })
    }
}

$data = [pscustomobject]@{
    course = [pscustomobject]@{
        title       = "МДК 09.01 Проектирование и разработка веб-приложений"
        description = "МДК 09.01 — проектирование и разработка веб-приложений: HTML, CSS, JavaScript, TypeScript, React, .NET, интеграция клиент-сервер, тестирование и развертывание. Специальность ИП."
    }
    teacherEmail = "ivanova@collegelms.ru"
    studentEmail = "student01@collegelms.ru"
    groupName    = "ИСП-31"
    lectures     = $lectures
}

$json = $data | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($OutPath, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Output "OK: $OutPath — занятий: $($lectures.Count)"
