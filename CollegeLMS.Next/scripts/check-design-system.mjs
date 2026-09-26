#!/usr/bin/env node
// Храповик качества дизайн-системы CollegeLMS.
//
// Скрипт НЕ блокирует работу над уже существующим состоянием: он сравнивает
// результат с файлом `design-system.baseline.json` и падает только на НОВЫЕ
// нарушения. Так старые проблемы видны и учтены, а регрессии ловятся сразу.
//
// Обновить baseline осознанно: `node scripts/check-design-system.mjs --update`

import { readFileSync, writeFileSync, existsSync, readdirSync } from "node:fs"
import { resolve, dirname, relative, sep } from "node:path"
import { fileURLToPath } from "node:url"

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..")
const BASELINE_FILE = resolve(ROOT, "design-system.baseline.json")
const updateMode = process.argv.includes("--update")

const SOURCE_ROOTS = ["app", "components", "hooks", "lib", "types"]
const IGNORED = new Set([".next", "node_modules", "playwright-report", "test-results"])
const SCAN_EXT = /\.(tsx|ts|css)$/

// Канонические имена токенов текущей системы (CollegeLMS.Next/app/globals.css).
// Раньше шкалы назывались fg/muted-fg/bg; правила проверяют именно их.
const FORBIDDEN = [
  {
    id: "no-raw-hex",
    hint: "цвет задан литералом — используйте токен из app/globals.css",
    // Только разметка и логика: в CSS значения токенов заданы литералами по определению.
    exts: [".tsx", ".ts"],
    test: (line) => /#[0-9a-fA-F]{3,8}\b/.test(line) && !/^\s*(\/\/|\*|\/\*)/.test(line),
  },
  {
    id: "no-transition-all",
    hint: "transition-all анимирует все свойства — перечислите нужные",
    exts: [".tsx"],
    test: (line) => /\btransition-all\b/.test(line),
  },
  {
    id: "no-raw-select",
    hint: "используйте UI-примитив Select вместо нативного select",
    exts: [".tsx"],
    test: (line) => /<select[\s>]/.test(line),
  },
  {
    id: "no-raw-table",
    hint: "используйте UI-примитив Table вместо нативного table",
    exts: [".tsx"],
    test: (line) => /<table[\s>]/.test(line),
  },
]

const REQUIRED_FILES = [
  "app/globals.css",
  "app/layout.tsx",
  "app/(public)/layout.tsx",
  "package.json",
  "playwright.config.ts",
  "components.json",
]

const FORBIDDEN_FILES = ["lib/design-provider.tsx"]

const TABLE_PRIMITIVE = resolve(ROOT, "components/ui/table.tsx")

function walk(dir, out = []) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (IGNORED.has(entry.name)) continue
    const full = resolve(dir, entry.name)
    if (entry.isDirectory()) walk(full, out)
    else if (SCAN_EXT.test(entry.name)) out.push(full)
  }
  return out
}

const findings = []
for (const id of FORBIDDEN_FILES) {
  if (existsSync(resolve(ROOT, id))) {
    findings.push({ rule: "forbidden-file", file: id, line: 1, hint: "устаревший файл должен быть удалён" })
  }
}
for (const id of REQUIRED_FILES) {
  if (!existsSync(resolve(ROOT, id))) {
    findings.push({ rule: "required-file", file: id, line: 1, hint: "обязательный файл отсутствует" })
  }
}

const files = SOURCE_ROOTS.flatMap((dir) => {
  const full = resolve(ROOT, dir)
  return existsSync(full) ? walk(full) : []
})

for (const file of files) {
  const rel = relative(ROOT, file).split(sep).join("/")
  const isTablePrimitive = file === TABLE_PRIMITIVE
  const lines = readFileSync(file, "utf8").split(/\r?\n/)
  for (const rule of FORBIDDEN) {
    if (isTablePrimitive && rule.id === "no-raw-table") continue
    if (rule.exts && !rule.exts.some((ext) => file.endsWith(ext))) continue
    lines.forEach((line, i) => {
      if (rule.test(line)) {
        findings.push({ rule: rule.id, file: rel, line: i + 1, hint: rule.hint })
      }
    })
  }
}

const key = (f) => `${f.rule}|${f.file}|${f.line}`
const baseline = existsSync(BASELINE_FILE) && !updateMode ? JSON.parse(readFileSync(BASELINE_FILE, "utf8")) : { findings: [] }
const known = new Set(baseline.findings.map(key))
const fresh = findings.filter((f) => !known.has(key(f)))
const current = new Set(findings.map(key))
const resolved = baseline.findings.filter((k) => !current.has(key(k)))

if (updateMode) {
  writeFileSync(
    BASELINE_FILE,
    `${JSON.stringify({ findings: findings.map(({ rule, file, line, hint }) => ({ rule, file, line, hint })) }, null, 2)}\n`,
  )
}

console.log("Проверка дизайн-системы CollegeLMS")
console.log(`просканировано файлов: ${files.length}`)
console.log(`известных нарушений (baseline): ${findings.length - fresh.length}`)
console.log(`новых нарушений: ${fresh.length}`)
if (resolved.length) console.log(`исчезнуло нарушений: ${resolved.length} (baseline можно обновить)`)
for (const f of fresh) {
  console.log(`  ${f.file}:${f.line} ${f.rule} ${f.hint}`)
}
if (fresh.length) {
  console.log(`\nПроверка не пройдена: новых нарушений ${fresh.length}.`)
  process.exit(1)
}
console.log("\nНовых нарушений нет.")
