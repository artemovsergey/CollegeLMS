# Button Patterns

## shadcn Button variants

```tsx
import { Button } from "@/components/ui/button"

// Primary (default)
<Button>Сохранить</Button>

// Secondary
<Button variant="secondary">Отмена</Button>

// Destructive
<Button variant="destructive">Удалить</Button>

// Outline
<Button variant="outline">Экспорт</Button>

// Ghost
<Button variant="ghost">Закрыть</Button>

// Link
<Button variant="link">Подробнее</Button>
```

## Sizes

```tsx
<Button size="sm">Маленькая</Button>
<Button size="default">Обычная</Button>
<Button size="lg">Большая</Button>
<Button size="icon"><Icon className="h-4 w-4" /></Button>
```

## With icon

```tsx
<Button><Plus className="mr-2 h-4 w-4" />Добавить</Button>
<Button><Download className="mr-2 h-4 w-4" />Скачать</Button>
```

## Loading state

```tsx
<Button disabled={isLoading}>
  {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
  {isLoading ? "Сохранение..." : "Сохранить"}
</Button>
```

## Full width

```tsx
<Button className="w-full">Войти</Button>
```

## Rules

- Primary action: `variant="default"` (one per form/section)
- Cancel/secondary: `variant="secondary"` or `variant="outline"`
- Delete: `variant="destructive"` with confirmation
- Icon-only: `size="icon"` with aria-label
- Touch target: minimum 44x44px
