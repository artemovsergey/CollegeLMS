import { chromium } from "@playwright/test"

import { readFileSync } from "node:fs"

const token = readFileSync("/tmp/opencode/.adm.token", "utf8")
const me = await (
  await fetch("https://stvcc.tech/api/auth/profile", { headers: { Authorization: `Bearer ${token}` } })
).json()

const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: 1600, height: 1100 } })
await page.goto("https://stvcc.tech/login")
await page.evaluate(
  ([t, u]) => {
    localStorage.setItem("token", t)
    localStorage.setItem("user", JSON.stringify(u))
  },
  [token, me.data]
)

async function open(url, file, wait = 1500) {
  await page.goto(url)
  await page.waitForLoadState("networkidle")
  await page.waitForTimeout(wait)
  const count = await page.locator(".prose").count()
  const card = await page.locator(".bg-card").first().boundingBox().catch(() => null)
  const box = card ? { width: card.width } : null
  console.log("  prose:", count, "url:", page.url())
  console.log(url.replace("https://stvcc.tech", ""), "→ ширина блока контента:", box ? Math.round(box.width) : "нет")
  await page.screenshot({ path: file })
}

await open("https://stvcc.tech/courses/ad4b07ad-55fe-422a-99ec-eaba038fda82/lessons/201a43c8-2c92-4dad-b3f5-89672bfd36de", "/tmp/opencode/w-lesson.png")
await open("https://stvcc.tech/courses/530759b3-d955-41b1-bd17-3ca4c0b44476/lessons/3ebed762-bc75-4845-b930-648381fbd4d6", "/tmp/opencode/w-lesson-img.png")
await open("https://stvcc.tech/courses/ad4b07ad-55fe-422a-99ec-eaba038fda82", "/tmp/opencode/w-course.png")

// как выглядят картинки в лекции
await page.goto("https://stvcc.tech/courses/530759b3-d955-41b1-bd17-3ca4c0b44476/lessons/3ebed762-bc75-4845-b930-648381fbd4d6")
await page.waitForLoadState("networkidle")
await page.locator(".prose img").first().scrollIntoViewIfNeeded()
await page.waitForTimeout(1000)
await page.screenshot({ path: "/tmp/opencode/w-img.png" })
console.log(
  "картинки:",
  JSON.stringify(
    await page.locator(".prose img").evaluateAll(els =>
      els.slice(0, 4).map(e => {
        const r = e.getBoundingClientRect()
        return { w: Math.round(r.width), h: Math.round(r.height), natural: `${e.naturalWidth}x${e.naturalHeight}` }
      })
    )
  )
)
await browser.close()
