# Card Patterns

## Basic card

```tsx
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"

<Card>
  <CardHeader>
    <CardTitle>Профиль студента</CardTitle>
    <CardDescription>Основная информация</CardDescription>
  </CardHeader>
  <CardContent>
    <p>Содержимое карточки</p>
  </CardContent>
  <CardFooter>
    <Button>Сохранить</Button>
  </CardFooter>
</Card>
```

## Stats card

```tsx
<Card>
  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
    <CardTitle className="text-sm font-medium">Всего студентов</CardTitle>
    <Users className="h-4 w-4 text-muted-foreground" />
  </CardHeader>
  <CardContent>
    <div className="text-2xl font-bold">1,234</div>
    <p className="text-xs text-muted-foreground">+12% за месяц</p>
  </CardContent>
</Card>
```

## Grid layout

```tsx
<div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
  <Card>...</Card>
  <Card>...</Card>
  <Card>...</Card>
</div>
```

## Rules

- Use for grouping related content
- Header with title + optional description
- Footer for actions (buttons)
- Consistent padding: `p-6` on CardContent
