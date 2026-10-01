import { defineConfig, devices } from "@playwright/test"

/**
 * Стенд живёт на том же хосте, что и рабочий каталог, и раздаётся
 * балансировщиком на 80-м порту: напрямую на :3000 пути /api/* отдают 404.
 * Поэтому адрес стенда задаётся переменной, а `next dev` не поднимается.
 */
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000"
const externalBaseURL = process.env.PLAYWRIGHT_BASE_URL != null

export default defineConfig({
  webServer: externalBaseURL
    ? undefined
    : {
        command: "npm run dev",
        url: "http://localhost:3000",
        reuseExistingServer: !process.env.CI,
        timeout: 180000,
      },
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? "html" : [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "mobile",
      use: { ...devices["Pixel 5"], viewport: { width: 393, height: 1366 } },
    },
  ],
})
