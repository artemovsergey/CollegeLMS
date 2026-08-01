# Аудит внутренних ссылок публичной части сайта (Next.js)
# Ищет href в page.tsx и data/*.ts, собирает уникальные внутренние ссылки
# и сверяет их с реальными роутами (файлы app/**/page.tsx).

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$next = Join-Path $root "CollegeLMS.Next"
$app = Join-Path $next "app"

# --- 1. Собрать реальные роуты из файлов app/**/page.tsx ---------------------
$routeFiles = Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object { $_.Name -eq "page.tsx" }
$routes = New-Object System.Collections.Generic.HashSet[string]
foreach ($f in $routeFiles) {
    $rel = $f.FullName.Substring($app.Length).Replace("\", "/")
    $rel = $rel -replace '/page\.tsx$', ''
    $rel = $rel -replace '/\(public\)', ''
    $rel = $rel -replace '/\(authenticated\)', ''
    if ($rel -eq "") { $rel = "/" }
    [void]$routes.Add($rel)
}

function Test-Route {
    param([string]$path)
    if ($routes.Contains($path)) { return $true }
    $pSegs = @($path.TrimStart('/').Split('/') | Where-Object { $_ -ne "" })
    foreach ($r in $routes) {
        $rSegs = @($r.TrimStart('/').Split('/') | Where-Object { $_ -ne "" })
        $match = $true
        for ($i = 0; $i -lt $rSegs.Length; $i++) {
            $seg = $rSegs[$i]
            if ($seg.StartsWith('[[...')) {
                # catch-all — совпадает с любым количеством оставшихся сегментов
                $match = $true
                break
            }
            if ($seg.StartsWith('[')) { continue }  # динамический сегмент [id]/[slug]
            if ($i -ge $pSegs.Length -or $seg -ne $pSegs[$i]) { $match = $false; break }
        }
        if ($match -and $pSegs.Length -eq $rSegs.Length) { return $true }
        if ($match -and $rSegs.Length -gt 0 -and $rSegs[$rSegs.Length - 1].StartsWith('[[...')) { return $true }
    }
    return $false
}

# --- 2. Собрать все href из page.tsx и data/*.ts -----------------------------
$srcFiles = Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object { $_.Name -eq "page.tsx" }
$srcFiles += Get-ChildItem -Path (Join-Path $next "data") -File -Filter "*.ts"

$found = @{}  # href -> список файлов
foreach ($f in $srcFiles) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $f.FullName
    if (-not $text) { continue }
    # Статические href="...", href='...' и href: "/..." (объекты данных)
    $ms = [regex]::Matches($text, 'href\s*[:=]\s*["''](/[^"''`$]+?)["'']')
    foreach ($m in $ms) {
        $href = $m.Groups[1].Value
        if ($href -match '^(#|/\/|javascript:)') { continue }
        if (-not $found.ContainsKey($href)) { $found[$href] = New-Object System.Collections.Generic.List[string] }
        $found[$href].Add($f.FullName.Replace($root + "\", ""))
    }
    # Шаблонные href={`/route/${...}`} — проверить статический префикс
    $ms = [regex]::Matches($text, 'href=\{?`(/[^`]+)`')
    foreach ($m in $ms) {
        $tmpl = $m.Groups[1].Value
        $prefix = ($tmpl -split '\$\{')[0]
        $prefix = $prefix.TrimEnd('/')
        if ($prefix -eq "" -or $prefix.StartsWith('/api')) { continue }
        if (-not $found.ContainsKey($prefix)) { $found[$prefix] = New-Object System.Collections.Generic.List[string] }
        $found[$prefix].Add($f.FullName.Replace($root + "\", "") + " (шаблон)")
    }
}

# --- 3. Проверка --------------------------------------------------------------
$report = @()
foreach ($href in ($found.Keys | Sort-Object)) {
    $ok = Test-Route $href
    if (-not $ok) {
        $report += [pscustomobject]@{
            Href  = $href
            Files = ($found[$href] -join "; ")
            Status = "BROKEN"
        }
    }
}

$json = if ($report.Count -gt 0) { $report | ConvertTo-Json -Depth 3 } else { "[]" }
$json | Set-Content (Join-Path $PSScriptRoot "content-links-report.json") -Encoding UTF8
Write-Output "Checked: $($found.Count) unique internal links, Broken: $($report.Count)"
foreach ($r in $report) { Write-Output "$($r.Href)  <=  $($r.Files)" }
