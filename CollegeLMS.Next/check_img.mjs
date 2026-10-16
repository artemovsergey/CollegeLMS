import { chromium } from "@playwright/test"
import { readFileSync } from "node:fs"

const token = readFileSync("/tmp/opencode/.adm.token", "utf8")
const me = await (await fetch("https://stvcc.tech/api/auth/profile", { headers: { Authorization: `Bearer ${token}` } })).json()
const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } })
await page.goto("https://stvcc.tech/login")
await page.evaluate(([t, u]) => { localStorage.setItem("token", t); localStorage.setItem("user", JSON.stringify(u)) }, [token, me.data])
await page.goto("https://stvcc.tech/courses/530759b3-d955-41b1-bd17-3ca4c0b44476/lessons/3ebed762-bc75-4845-b930-648381fbd4d6")
await page.waitForLoadState("networkidle")
const tall = page.locator('.prose img[src*="01-start"]')
await tall.scrollIntoViewIfNeeded()
await page.waitForTimeout(800)
await page.screenshot({ path: "/tmp/opencode/img-tall.png" })
console.log(await tall.evaluate(e => ({ w: Math.round(e.getBoundingClientRect().width), h: Math.round(e.getBoundingClientRect().height), nat: `${e.naturalWidth}x${e.naturalHeight}` })))
await browser.close()
