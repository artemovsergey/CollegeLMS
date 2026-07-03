# Modal / Dialog Patterns

## Basic dialog

```tsx
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"

<Dialog>
  <DialogTrigger asChild>
    <Button>Открыть</Button>
  </DialogTrigger>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Заголовок</DialogTitle>
      <DialogDescription>Описание диалога</DialogDescription>
    </DialogHeader>
    <div className="py-4">
      {/* Content */}
    </div>
    <DialogFooter>
      <Button variant="outline">Отмена</Button>
      <Button>Подтвердить</Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```

## Confirmation dialog

```tsx
<DialogContent>
  <DialogHeader>
    <DialogTitle>Удалить запись?</DialogTitle>
    <DialogDescription>
      Это действие нельзя отменить. Запись будет удалена навсегда.
    </DialogDescription>
  </DialogHeader>
  <DialogFooter>
    <Button variant="outline" onClick={onClose}>Отмена</Button>
    <Button variant="destructive" onClick={onDelete}>Удалить</Button>
  </DialogFooter>
</DialogContent>
```

## Rules

- Escape key closes dialog
- Click outside closes dialog
- Focus trapped inside dialog
- Return focus to trigger on close
- Max width: `sm:max-w-lg` for forms, `sm:max-w-xl` for content
- Always have visible close button (X icon)
