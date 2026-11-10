# Table Patterns

## Basic table

```tsx
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"

<Table>
  <TableHeader>
    <TableRow>
      <TableHead>Имя</TableHead>
      <TableHead>Группа</TableHead>
      <TableHead>Оценка</TableHead>
    </TableRow>
  </TableHeader>
  <TableBody>
    {items.map((item) => (
      <TableRow key={item.id}>
        <TableCell>{item.name}</TableCell>
        <TableCell>{item.group}</TableCell>
        <TableCell>{item.grade}</TableCell>
      </TableRow>
    ))}
  </TableBody>
</Table>
```

## With empty state

```tsx
{items.length === 0 ? (
  <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
    <Inbox className="h-12 w-12 mb-4" />
    <p>Нет данных для отображения</p>
  </div>
) : (
  <Table>...</Table>
)}
```

## With actions column

```tsx
<TableRow>
  <TableCell>{item.name}</TableCell>
  <TableCell className="text-right">
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent>
        <DropdownMenuItem>Редактировать</DropdownMenuItem>
        <DropdownMenuItem className="text-destructive">Удалить</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  </TableCell>
</TableRow>
```

## Rules

- Use for lists of structured data
- Sortable columns: add `onClick` + sort indicator
- Empty state always shown when no data
- Actions column: right-aligned, icon-only button
- Responsive: horizontal scroll on mobile (`overflow-x-auto`)
