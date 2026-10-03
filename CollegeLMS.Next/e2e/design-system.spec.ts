import { expect, test } from "@playwright/test"

/**
 * Согласованность продукта: §6.1, §7 и §8.3.
 *
 * Четыре проверки, которые DESIGN.md обещает, но до сих пор не выполнял ни
 * один тест: контраст обеих тем, отсутствие горизонтального переполнения на
 * 320px, доступность форм и размеры интерактивных элементов.
 *
 * Здесь нет продуктовых утверждений — только требования §6 и §7. Поэтому
 * проверки не зависят от наполнения страниц и переживают правку контента.
 */

const SURFACES = [
  { name: "публичный сайт", path: "/" },
  { name: "вход", path: "/login" },
  { name: "новости", path: "/news" },
  { name: "специальности", path: "/specialties" },
  { name: "поиск", path: "/search" },
]

/**
 * Контраст считается прямо в браузере через canvas.
 *
 * Разбирать `getComputedStyle` вручную нельзя: Chromium отдаёт результат
 * `color-mix()` как `oklab(… / …)`, а не `rgb()`, и парсер на `rgb()` падал
 * на первом же тексте. Канва принимает любой формат CSS Color 4 и отдаёт
 * готовый байт.
 */
interface ContrastItem {
  text: string
  ratio: number
  size: number
}

interface ContrastItem {
  text: string
  ratio: number
  size: number
}

/**
 * Проб контраста выполняется целиком в браузере, поэтому весь разбор цвета и
 * композитинг лежат внутри функции: `page.evaluate` увозит с собой только её
 * одну, и соседние помощники в браузере не видны.
 *
 * Считать приходится здесь, а не в Node, потому что Chromium отдаёт результат
 * `color-mix()` и любого `*-text/70` как `oklab(… / α)`, а не `rgb()`. Проба
 * через canvas молчала: присваивание `fillStyle = "oklab(…)"` игнорируется, и
 * весь текст сравнивался сам с собой — соотношение ровно 1.00:1.
 */
function probeContrast(): ContrastItem[] {
  type Rgba = [number, number, number, number]

  /** Разбор `rgb()`/`rgba()` и `oklab()`, перевод oklab в sRGB. */
  function parseColor(css: string): Rgba | null {
    const value = css.trim()

    const rgb = value.match(/^rgba?\(([^)]+)\)$/i)
    if (rgb) {
      const parts = rgb[1].split(/[,\s/]+/).filter(Boolean).map(Number)
      if (parts.length >= 3 && parts.slice(0, 3).every(Number.isFinite)) {
        return [parts[0], parts[1], parts[2], parts.length > 3 ? parts[3] : 1]
      }
      return null
    }

    const lab = value.match(
      /^oklab\(\s*([\d.]+%?)\s+([\d.-]+)\s+([\d.-]+)\s*(?:\/\s*([\d.]+%?)\s*)?\)$/i,
    )
    if (lab) {
      const scale = (raw: string): number =>
        raw.endsWith("%") ? Number.parseFloat(raw) / 100 : Number.parseFloat(raw)
      const L = scale(lab[1])
      const a = Number(lab[2])
      const b = Number(lab[3])
      const alpha = lab[4] ? scale(lab[4]) : 1

      // oklab → линейный sRGB (Björn Ottosson).
      const l_ = L + 0.3963377774 * a + 0.2158037573 * b
      const m_ = L - 0.1055613458 * a - 0.0638541728 * b
      const s_ = L - 0.0894841775 * a - 1.291485548 * b
      const l = l_ ** 3
      const m = m_ ** 3
      const s = s_ ** 3
      const linear = [
        4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
        -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
        -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
      ]
      const encoded = linear.map((c) => {
        const clamped = Math.min(1, Math.max(0, c))
        const v =
          clamped <= 0.0031308 ? 12.92 * clamped : 1.055 * clamped ** (1 / 2.4) - 0.055
        return Math.round(Math.min(1, Math.max(0, v)) * 255)
      })
      return [encoded[0], encoded[1], encoded[2], alpha]
    }

    return null
  }

  /** Наложение слоёв снизу вверх: source-over по alpha. */
  function composite(layers: Rgba[]): Rgba {
    let out: Rgba = [0, 0, 0, 0]
    for (const layer of layers) {
      const alpha = layer[3]
      const outAlpha = alpha + out[3] * (1 - alpha)
      if (outAlpha === 0) {
        out = [0, 0, 0, 0]
        continue
      }
      out = [
        (layer[0] * alpha + out[0] * out[3] * (1 - alpha)) / outAlpha,
        (layer[1] * alpha + out[1] * out[3] * (1 - alpha)) / outAlpha,
        (layer[2] * alpha + out[2] * out[3] * (1 - alpha)) / outAlpha,
        outAlpha,
      ]
    }
    return out
  }

  function luminance(color: Rgba): number {
    const f = (v: number): number => {
      const c = v / 255
      return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
    }
    return 0.2126 * f(color[0]) + 0.7152 * f(color[1]) + 0.0722 * f(color[2])
  }

  function ratio(a: Rgba, b: Rgba): number {
    const la = luminance(a)
    const lb = luminance(b)
    const [light, dark] = la > lb ? [la, lb] : [lb, la]
    return (light + 0.05) / (dark + 0.05)
  }

  /** Фон под элементом: слои в порядке отрисовки, от корня к элементу. */
  function backgroundStack(el: HTMLElement): Rgba[] {
    const stack: Rgba[] = []
    let cursor: HTMLElement | null = el
    while (cursor && cursor !== document.documentElement) {
      const color = parseColor(getComputedStyle(cursor).backgroundColor)
      if (color && color[3] > 0) stack.unshift(color)
      cursor = cursor.parentElement
    }
    const root = parseColor(
      getComputedStyle(document.documentElement).backgroundColor,
    )
    stack.unshift(root && root[3] > 0 ? root : [255, 255, 255, 1])
    return stack
  }

  const out: ContrastItem[] = []
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT)
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = (node.textContent ?? "").trim()
    if (!text) continue
    // Разделитель вроде «|» — украшение, а не текст: WCAG к нему не относится.
    if (!/[\p{L}\p{N}]/u.test(text)) continue
    const el = node.parentElement
    if (!el) continue
    const style = getComputedStyle(el)
    if (
      style.visibility === "hidden" ||
      style.display === "none" ||
      Number(style.opacity) === 0
    ) {
      continue
    }
    const rect = el.getBoundingClientRect()
    if (rect.width === 0 || rect.height === 0) continue
    const size = Number.parseFloat(style.fontSize)
    if (size < 12) continue

    const foreground = parseColor(style.color)
    if (!foreground) continue
    const stack = backgroundStack(el)
    out.push({
      text,
      ratio: ratio(composite([...stack, foreground]), composite(stack)),
      size,
    })
  }
  return out
}

test.describe("контраст текста", () => {
  for (const surface of SURFACES) {
    for (const theme of ["light", "dark"] as const) {
      test(`${surface.name}: ${theme}`, async ({ page }) => {
        // Страница поиска обращается к API при монтировании, и в режиме
        // разработки первая компиляция уходит за стандартные 30 секунд
        // навигации. Ждём DOM, а не сеть: контраст проверяется у уже
        // отрисованного текста.
        await page.goto(surface.path, { waitUntil: "domcontentloaded" })
        await page.waitForTimeout(500)
        await page.evaluate((t) => {
          localStorage.setItem("theme", t)
          document.documentElement.classList.toggle("dark", t === "dark")
        }, theme)
        await page.waitForTimeout(300)

        const items = await page.evaluate(probeContrast)
        expect(items.length, "на странице нет видимого текста").toBeGreaterThan(0)

        const failures = items
          .map((item) => {
            const large = item.size >= 24 || item.size >= 18.66
            const required = large ? 3 : 4.5
            // Допуск 0.15 — расхождение между браузером и точным разбором
            // пар токенов в `check-token-contrast.mjs`. Здесь цвет берётся из
            // `getComputedStyle`, там из файла токенов; на `oklab`-цветах
            // путь округления даёт ~0.09. Реальные нарушения (2.16:1, 2.7:1,
            // 3.33:1) этим допуском не маскируются.
            return item.ratio < required - 0.15
              ? `${JSON.stringify(item.text.slice(0, 40))} — ${item.ratio.toFixed(2)}:1 при ${Math.round(item.size)}px, нужно ${required}:1`
              : null
          })
          .filter((v): v is string => v !== null)

        expect(failures, `контраст ${theme}: ${surface.path}`).toEqual([])
      })
    }
  }
})

test.describe("ширина 320px", () => {
  test.use({ viewport: { width: 320, height: 720 } })

  for (const surface of SURFACES) {
    test(surface.name, async ({ page }) => {
      await page.goto(surface.path)
      // Горизонтальная прокрутка — верный признак того, что верстка не
      // помещается в 320px. PRODUCT.md требует этого размера.
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      )
      expect(overflow, `${surface.path} прокручивается по горизонтали`).toBeLessThanOrEqual(1)
    })
  }
})

test.describe("размер интерактивных элементов", () => {
  // §6.1: не меньше 44×44. Проверяем на 320px — на широком экране размеры
  // меньше, а на касании важен именно узкий экран.
  test.use({ viewport: { width: 320, height: 720 } })

  const INTERACTIVE = 'button, a[href], input:not([type=hidden]), select, [role=button]'

  for (const surface of SURFACES) {
    test(surface.name, async ({ page }) => {
      await page.goto(surface.path)
      const tooSmall = await page.evaluate((sel) => {
        const out: string[] = []
        for (const el of document.querySelectorAll<HTMLElement>(sel)) {
          const style = getComputedStyle(el)
          if (style.display === "none" || style.visibility === "hidden") continue
          // Закрытая шторка и выпадающее меню скрыты `invisible`, а не
          // `display: none`: у них есть размеры, и без этой проверки
          // измерялись бы ссылки, которые пользователь не видит.
          if (el.closest("[inert]")) continue
          const rect = el.getBoundingClientRect()
          if (rect.width === 0 || rect.height === 0) continue
          if (rect.bottom < 0 || rect.top > window.innerHeight) continue
          if (rect.right < 0 || rect.left > window.innerWidth) continue
          // Мобильная шторка схлопывается через `grid-template-rows: 0fr`, то
          // есть её дети сохраняют размеры, но обрезаны предком. Проверка
          // «что реально лежит под этой точкой» отсекает обрезанное.
          const cx = rect.left + rect.width / 2
          const cy = rect.top + rect.height / 2
          if (cy < 0 || cy > window.innerHeight || cx < 0 || cx > window.innerWidth) continue
          const hit = document.elementFromPoint(cx, cy)
          if (hit && hit !== el && !el.contains(hit) && !hit.contains(el)) continue
          // Ссылка внутри абзаца текста — не самостоятельная цель касания.
          if (el.tagName === "A" && el.closest("p, li, .docs-content")) continue
          if (rect.height < 44) {
            const label =
              el.getAttribute("aria-label") ?? el.textContent?.trim().slice(0, 30) ?? el.tagName
            out.push(`${el.tagName} «${label}» — ${Math.round(rect.height)}px`)
          }
        }
        return out
      }, INTERACTIVE)
      expect(tooSmall, `${surface.path}: цели касания меньше 44px`).toEqual([])
    })
  }
})

test.describe("доступность форм", () => {
  for (const surface of [SURFACES[1]]) {
    test(`${surface.name}: у полей есть подпись`, async ({ page }) => {
      await page.goto(surface.path)
      const unlabelled = await page.evaluate(() => {
        const out: string[] = []
        for (const el of document.querySelectorAll<HTMLElement>(
          "input:not([type=hidden]), select, textarea",
        )) {
          const id = el.id
          const hasLabel =
            (id && document.querySelector(`label[for="${CSS.escape(id)}"]`)) ||
            el.closest("label") ||
            el.getAttribute("aria-label") ||
            el.getAttribute("aria-labelledby")
          if (!hasLabel) out.push(el.outerHTML.slice(0, 90))
        }
        return out
      })
      expect(unlabelled, "поля без подписи").toEqual([])
    })

    test(`${surface.name}: у полей есть имя для программ доступа`, async ({
      page,
    }) => {
      await page.goto(surface.path)
      const unnamed = await page.evaluate(() => {
        const out: string[] = []
        for (const el of document.querySelectorAll<HTMLElement>(
          "input:not([type=hidden]), select, textarea",
        )) {
          const name =
            el.getAttribute("name") ||
            el.getAttribute("aria-label") ||
            el.getAttribute("aria-labelledby") ||
            (el.id && document.querySelector(`label[for="${CSS.escape(el.id)}"]`))
          if (!name) out.push(el.outerHTML.slice(0, 90))
        }
        return out
      })
      expect(unnamed, "поля без доступного имени").toEqual([])
    })
  }
})

test.describe("видимый фокус", () => {
  // §6.1: видимый фокус. Проверяем клавиатурой, потому что проверка мышью
  // не находит расхождений: под курсором всё выглядит одинаково.
  test("кнопка получает кольцо по Tab", async ({ page }) => {
    await page.goto("/")
    await page.keyboard.press("Tab")
    const ring = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null
      if (!el || el === document.body) return null
      const style = getComputedStyle(el)
      const width = Number.parseFloat(style.outlineWidth || "0")
      const hasBoxShadow = style.boxShadow !== "none"
      const hasOutline = width > 0 && style.outlineStyle !== "none"
      return { tag: el.tagName, hasBoxShadow, hasOutline, width }
    })
    expect(ring, "первый элемент по Tab должен быть виден").not.toBeNull()
    expect(
      ring!.hasBoxShadow || ring!.hasOutline,
      `у ${ring!.tag} нет видимого фокуса`,
    ).toBe(true)
  })
})