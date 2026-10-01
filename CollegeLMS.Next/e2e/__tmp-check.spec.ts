import { test } from '@playwright/test'

test('перенос виден в итоговом предпросмотре и в XLSX', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Логин').fill('dispatcher')
  await page.getByLabel('Пароль').fill('dispatcher')
  await page.getByRole('button', { name: /^Войти$/ }).click()
  await page.waitForTimeout(3500)
  await page.goto('/dispatcher/correction')
  await page.waitForTimeout(3000)
  await page.getByRole('button', { name: 'Открыть пакет за 01.10.2026' }).first().click()
  await page.waitForTimeout(3000)

  await page.getByRole('button', { name: /Предпросмотр/ }).first().click()
  await page.waitForTimeout(1800)
  const d = page.locator('[role=dialog]').first()
  console.log('ИТОГОВЫЙ ПРЕДПРОСМОТР:', (await d.innerText()).replace(/\n+/g, ' | ').slice(0, 600))
  await page.screenshot({ path: 'test-results/file-preview.png' })
  await d.keyboard.press('Escape')
  await page.waitForTimeout(1000)

  // скачиваем XLSX пакета
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'XLSX' }).click(),
  ])
  const path = await download.path()
  console.log('XLSX:', path)
})
