# Badge Patterns

## Variants

```tsx
import { Badge } from "@/components/ui/badge"

<Badge>По умолчанию</Badge>
<Badge variant="secondary">Вторичный</Badge>
<Badge variant="outline">Контурный</Badge>
<Badge variant="destructive">Ошибка</Badge>
```

## Semantic usage

```tsx
// Status badges
<Badge variant="default">Активный</Badge>
<Badge variant="secondary">Ожидание</Badge>
<Badge variant="destructive">Заблокирован</Badge>
<Badge variant="outline">Черновик</Badge>

// Role badges
<Badge variant="default">Администратор</Badge>
<Badge variant="secondary">Преподаватель</Badge>
<Badge variant="outline">Студент</Badge>

// Grade badges
<Badge variant="default">Отлично</Badge>
<Badge variant="secondary">Хорошо</Badge>
<Badge variant="outline">Удовлетворительно</Badge>
<Badge variant="destructive">Неудовлетворительно</Badge>
```

## With icon

```tsx
<Badge variant="default"><CheckCircle className="mr-1 h-3 w-3" />Активный</Badge>
<Badge variant="destructive"><XCircle className="mr-1 h-3 w-3" />Ошибка</Badge>
```

## Rules

- Use for short status/label text
- Keep text concise (1-2 words)
- Color conveys meaning: green=success, red=error, gray=neutral
- Do not use for long text or descriptions
