# Input Patterns

## Basic input

```tsx
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

<div className="space-y-2">
  <Label htmlFor="name">Имя</Label>
  <Input id="name" placeholder="Введите имя" />
</div>
```

## With description

```tsx
<div className="space-y-2">
  <Label htmlFor="email">Email</Label>
  <Input id="email" type="email" placeholder="user@example.com" />
  <p className="text-sm text-muted-foreground">Мы не передаём email третьим лицам</p>
</div>
```

## With error

```tsx
<div className="space-y-2">
  <Label htmlFor="password">Пароль</Label>
  <Input id="password" type="password" className="border-destructive" />
  <p className="text-sm text-destructive">Пароль должен содержать минимум 8 символов</p>
</div>
```

## Select

```tsx
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

<Select>
  <SelectTrigger>
    <SelectValue placeholder="Выберите группу" />
  </SelectTrigger>
  <SelectContent>
    <SelectItem value="group-1">Группа 1</SelectItem>
    <SelectItem value="group-2">Группа 2</SelectItem>
  </SelectContent>
</Select>
```

## Textarea

```tsx
import { Textarea } from "@/components/ui/textarea"

<div className="space-y-2">
  <Label htmlFor="notes">Примечание</Label>
  <Textarea id="notes" placeholder="Добавьте примечание..." />
</div>
```

## Rules

- Labels always visible (not placeholder-only)
- Error state: red border + message below
- Required: mark with `*` or aria-required
- Placeholder in Russian
- `htmlFor` + `id` pairing for accessibility
