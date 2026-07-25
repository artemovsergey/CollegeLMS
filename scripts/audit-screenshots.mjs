import { chromium } from "playwright"
const BASE = "http://localhost:3000"
const pages = [
  { path: "/", name: "homepage" },
  { path: "/news", name: "news-list" },
  { path: "/login", name: "login" },
]
async function main() {
  const browser = await chromium.launch()
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } })
  const page = await context.newPage()
  for (const { path, name } of pages) {
    await page.goto(`${BASE}${path}`, { waitUntil: "networkidle" })
    await page.screenshot({ path: `docs/screenshots/${name}.png`, fullPage: true })
    console.log(`✓ ${name}`)
  }
  await browser.close()
}
main().catch(console.error)
