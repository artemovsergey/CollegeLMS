#!/usr/bin/env node
// Контраст токенов CollegeLMS.
//
// Токены текущей системы живут в app/globals.css (блоки `:root`, `.dark`) и
// в app/max/max.css (`.max-app`, `.dark .max-app`). Скрипт считает контраст
// WCAG 2.1 по каноническим парам и сравнивает результат с baseline — падает
// только на НОВЫЕ нарушения, чтобы старые дефекты были видны, но не блокировали.
//
//   node scripts/check-token-contrast.mjs            проверка с baseline
//   node scripts/check-token-contrast.mjs --report   markdown-таблицы, код 0
//   node scripts/check-token-contrast.mjs --update   перезаписать baseline

import { readFileSync, writeFileSync, existsSync } from "node:fs"
import { resolve, dirname } from "node:path"
import { fileURLToPath } from "node:url"

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..")
const BASELINE_FILE = resolve(ROOT, "design-system-contrast.baseline.json")
const updateMode = process.argv.includes("--update")
const reportMode = process.argv.includes("--report")

const RATIO = { text: 4.5, nonText: 3 }

const globals = readFileSync(resolve(ROOT, "app/globals.css"), "utf8")
const maxSource = existsSync(resolve(ROOT, "app/max/max.css"))
  ? readFileSync(resolve(ROOT, "app/max/max.css"), "utf8")
  : ""

function parseBlock(source, selector) {
  // Ищем именно объявление блока, а не любое упоминание селектора:
  // `@custom-variant dark (&:is(.dark *))` идёт раньше `.dark {` в globals.css.
  const pattern = new RegExp(`^[^\\S\\n]*${selector.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\s*\\{`, "m")
  const match = pattern.exec(source)
  if (!match) return new Map()
  const open = match.index + match[0].length - 1
  let depth = 0
  let end = open
  for (let i = open; i < source.length; i += 1) {
    if (source[i] === "{") depth += 1
    else if (source[i] === "}") {
      depth -= 1
      if (depth === 0) {
        end = i
        break
      }
    }
  }
  const map = new Map()
  const body = source.slice(open + 1, end)
  const re = /(--[a-z0-9-]+)\s*:\s*([^;]+);/gi
  let m
  while ((m = re.exec(body)) !== null) map.set(m[1], m[2].trim())
  return map
}

function parseColor(value) {
  if (!value) return null
  const v = value.trim()
  const hex = v.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/i)
  if (hex) {
    const h = hex[1].length === 3 ? hex[1].split("").map((c) => c + c).join("") : hex[1]
    return [0, 2, 4].map((i) => parseInt(h.slice(i, i + 2), 16))
  }
  const rgb = v.match(/^rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)/i)
  if (rgb) return [Number(rgb[1]), Number(rgb[2]), Number(rgb[3])]
  const hsl = v.match(/^hsla?\(\s*([\d.]+)[,\s]+([\d.]+)%[,\s]+([\d.]+)%/i)
  if (hsl) {
    const h = Number(hsl[1]) / 360
    const s = Number(hsl[2]) / 100
    const l = Number(hsl[3]) / 100
    if (s === 0) {
      const g = Math.round(l * 255)
      return [g, g, g]
    }
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s
    const p = 2 * l - q
    const channelOf = (t) => {
      let x = t
      if (x < 0) x += 1
      if (x > 1) x -= 1
      if (x < 1 / 6) return p + (q - p) * 6 * x
      if (x < 1 / 2) return q
      if (x < 2 / 3) return p + (q - p) * (2 / 3 - x) * 6
      return p
    }
    return [channelOf(h + 1 / 3), channelOf(h), channelOf(h - 1 / 3)].map((c) => Math.round(c * 255))
  }
  const ref = v.match(/^var\(\s*(--[a-z0-9-]+)\s*\)$/i)
  if (ref) return parseColor(resolveVar(ref[1]))
  return null
}

const scopes = {
  root: parseBlock(globals, ":root"),
  dark: parseBlock(globals, ".dark"),
  a11y: parseBlock(globals, ".accessibility-mode"),
}
// MAX: светлая палитра в `.max-app`, тёмная — внутри
// `@media (prefers-color-scheme: dark)`. Это значит, что MAX не следует
// переключателю темы приложения (`.dark` на <html>) — учитываем это в отчёте.
const maxMediaAt = maxSource.indexOf("@media (prefers-color-scheme: dark)")
const maxLight = parseBlock(maxSource, ".max-app")
const maxDark = maxMediaAt === -1 ? new Map() : parseBlock(maxSource.slice(maxMediaAt), ".max-app")
scopes.maxLight = new Map([...scopes.root, ...maxLight])
scopes.maxDark = new Map([...scopes.dark, ...maxDark])

function resolveVar(name, scope = "root", depth = 0) {
  if (depth > 8) return null
  const value = scopes[scope]?.get(name)
  if (value === undefined) return null
  const ref = value.match(/^var\(\s*(--[a-z0-9-]+)\s*\)$/i)
  if (ref) return resolveVar(ref[1], scope, depth + 1)
  return value
}

// Режим высокой контрастности не переопределяет токены, а ломает оформление
// принудительно (body/[class*=bg-] → #fff, [class*=text-] → #000). Поэтому
// проверяем фактический результат этих правил, а не токены.
scopes.a11y = new Map([
  ["--bg", "#ffffff"],
  ["--fg", "#000000"],
  ["--muted", "#ffffff"],
  ["--muted-fg", "#000000"],
  ["--muted-foreground", "#000000"],
  ["--primary", "#000000"],
  ["--primary-foreground", "#ffffff"],
])

const PAIRS = {
  root: [
    ["fg", "bg", "text"],
    ["fg", "background", "text"],
    ["fg", "muted", "text"],
    ["muted-fg", "muted", "text"],
    ["muted-foreground", "background", "text"],
    ["card-foreground", "card", "text"],
    ["popover-foreground", "popover", "text"],
    ["primary-foreground", "primary", "text"],
    ["secondary-foreground", "secondary", "text"],
    ["accent-foreground", "accent", "text"],
    ["destructive-foreground", "destructive", "text"],
    ["success-foreground", "success", "text"],
    ["warning-foreground", "warning", "text"],
    ["ring", "background", "nonText"],
    ["input", "background", "nonText"],
  ],
  dark: [
    ["fg", "bg", "text"],
    ["fg", "background", "text"],
    ["fg", "muted", "text"],
    ["muted-fg", "muted", "text"],
    ["muted-foreground", "background", "text"],
    ["card-foreground", "card", "text"],
    ["popover-foreground", "popover", "text"],
    ["primary-foreground", "primary", "text"],
    ["secondary-foreground", "secondary", "text"],
    ["accent-foreground", "accent", "text"],
    ["destructive-foreground", "destructive", "text"],
    ["success-foreground", "success", "text"],
    ["warning-foreground", "warning", "text"],
    ["ring", "background", "nonText"],
    ["input", "background", "nonText"],
  ],
  a11y: [
    ["fg", "bg", "text"],
    ["muted-fg", "muted", "text"],
    ["primary-foreground", "primary", "text"],
  ],
  maxLight: [["text-primary", "background-surface-ground", "text"], ["text-secondary", "background-surface-ground", "text"]],
  maxDark: [["text-primary", "background-surface-ground", "text"], ["text-secondary", "background-surface-ground", "text"]],
}

const LABELS = {
  root: "светлая тема (:root)",
  dark: "тёмная тема (.dark)",
  a11y: "высокая контрастность",
  maxLight: "MAX (.max-app)",
  maxDark: "MAX (.dark .max-app)",
}

function channel(c) {
  const s = c / 255
  return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4
}
function luminance([r, g, b]) {
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
}
function contrast(fg, bg) {
  const a = luminance(fg)
  const b = luminance(bg)
  const [hi, lo] = a > b ? [a, b] : [b, a]
  return (hi + 0.05) / (lo + 0.05)
}
function hex([r, g, b]) {
  return `#${[r, g, b].map((v) => Math.round(v).toString(16).padStart(2, "0")).join("")}`
}

const rows = []
for (const [scope, pairs] of Object.entries(PAIRS)) {
  for (const [fgName, bgName, kind] of pairs) {
    const fgRaw = resolveVar(`--${fgName}`, scope)
    const bgRaw = resolveVar(`--${bgName}`, scope)
    const fg = parseColor(fgRaw)
    const bg = parseColor(bgRaw)
    const entry = { scope: LABELS[scope], pair: `${fgName} / ${bgName}`, kind, min: RATIO[kind] }
    if (!fg || !bg) {
      Object.assign(entry, { missing: true, pass: false })
    } else {
      const ratio = contrast(fg, bg)
      Object.assign(entry, { fg: hex(fg), bg: hex(bg), ratio: Number(ratio.toFixed(2)), pass: ratio >= RATIO[kind] })
    }
    rows.push(entry)
  }
}

const key = (r) => `${r.scope}|${r.pair}`
const baseline = existsSync(BASELINE_FILE) && !updateMode
  ? JSON.parse(readFileSync(BASELINE_FILE, "utf8"))
  : { findings: [] }
const known = new Set(baseline.findings.map(key))
const fresh = rows.filter((r) => !r.pass && !known.has(key(r)))
const failing = new Set(rows.filter((r) => !r.pass).map(key))
const resolved = baseline.findings.filter((k) => !failing.has(key(k)))

if (updateMode) {
  writeFileSync(
    BASELINE_FILE,
    `${JSON.stringify({ findings: rows.filter((r) => !r.pass).map(({ scope, pair, fg, bg, ratio, min }) => ({ scope, pair, fg, bg, ratio, min })) }, null, 2)}\n`,
  )
}

if (reportMode) {
  console.log("| Пара | Передний план | Фон | Контраст | Порог | Итог |")
  console.log("| --- | --- | --- | --- | --- | --- |")
  for (const r of rows) {
    const verdict = r.missing ? "ПРОБЕЛ" : r.pass ? "AA" : "ФЕЙЛ"
    console.log(`| ${r.pair} (${r.scope}) | ${r.fg ?? "—"} | ${r.bg ?? "—"} | ${r.ratio ? `${r.ratio}:1` : "—"} | ${r.min}:1 | ${verdict} |`)
  }
  process.exit(0)
}

console.log("Контраст токенов CollegeLMS")
for (const [scope, label] of Object.entries(LABELS)) {
  const scoped = rows.filter((r) => r.scope === label)
  const failed = scoped.filter((r) => !r.pass)
  console.log(`${label}: ${scoped.length - failed.length}/${scoped.length} пар проходят`)
}
console.log(`известных нарушений (baseline): ${rows.filter((r) => !r.pass).length - fresh.length}`)
console.log(`новых нарушений: ${fresh.length}`)
if (resolved.length) console.log(`исчезнуло нарушений: ${resolved.length} (baseline можно обновить)`)
for (const r of fresh) {
  console.log(`  ФЕЙЛ    [${r.scope}] ${r.pair} = ${r.ratio}:1 < ${r.min}:1  (${r.fg} на ${r.bg})`)
}
if (fresh.length) {
  console.log(`\nПроверка не пройдена: новых нарушений ${fresh.length}.`)
  process.exit(1)
}
console.log("\nНовых контрастных нарушений нет.")
