# Toast / Notification Patterns

## Basic toast (using sonner)

```tsx
import { toast } from "sonner"

// Success
toast.success("Студент добавлен")

// Error
toast.error("Ошибка сохранения")

// Info
toast.info("Изменения сохранены")

// With description
toast("Сохранено", {
  description: "Данные успешно обновлены",
})
```

## Promise toast

```tsx
toast.promise(saveData(), {
  loading: "Сохранение...",
  success: "Данные сохранены",
  error: "Ошибка сохранения",
})
```

## Custom action

```tsx
toast("Не удалось загрузить файл", {
  action: {
    label: "Повторить",
    onClick: () => retryUpload(),
  },
})
```

## Rules

- Success: green, auto-dismiss (3s)
- Error: red, manual dismiss
- Info: blue, auto-dismiss (5s)
- Max 3 visible toasts at once
- Position: bottom-right (default)
- Messages in Russian
- Keep messages short (1 line)
