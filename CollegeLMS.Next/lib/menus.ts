import {
  Users,
  Newspaper,
  MessageSquare,
  BookOpen,
  UsersRound,
  GraduationCap,
  CalendarDays,
  BookType,
  BadgeInfo,
  ClipboardCheck,
  Banknote,
  type LucideIcon,
} from "lucide-react"

export interface MenuItem {
  href: string
  label: string
  icon?: LucideIcon
}

export interface MenuSection {
  label: string
  items: MenuItem[]
}

export const adminMenuSections: MenuSection[] = [
  {
    label: "Система",
    items: [
      { href: "/admin", label: "Пользователи", icon: Users },
      { href: "/admin/news", label: "Новости", icon: Newspaper },
      { href: "/admin/feedback", label: "Обратная связь", icon: MessageSquare },
    ],
  },
  {
    label: "Обучение",
    items: [
      { href: "/courses", label: "Курсы", icon: BookOpen },
      { href: "/groups", label: "Группы", icon: UsersRound },
      { href: "/teachers", label: "Преподаватели", icon: GraduationCap },
      { href: "/students", label: "Студенты", icon: Users },
      { href: "/admin/semesters", label: "Семестры", icon: CalendarDays },
      { href: "/admin/specialties", label: "Специальности", icon: BadgeInfo },
      { href: "/admin/exams", label: "Экзамены", icon: ClipboardCheck },
      { href: "/admin/testing", label: "Тесты", icon: BookType },
    ],
  },
  {
    label: "Финансы",
    items: [{ href: "/admin/stipends", label: "Стипендии", icon: Banknote }],
  },
  {
    label: "Расписание",
    items: [{ href: "/schedule", label: "Расписание", icon: CalendarDays }],
  },
]

export const adminRoleMap: Record<string, string[]> = {
  "/admin": ["Admin"],
  "/admin/news": ["Admin", "Dispatcher"],
  "/admin/feedback": ["Admin"],
  "/admin/import": ["Admin"],
  "/courses": ["Admin", "Teacher"],
  "/groups": ["Admin"],
  "/teachers": ["Admin"],
  "/students": ["Admin"],
  "/admin/semesters": ["Admin"],
  "/admin/specialties": ["Admin"],
  "/admin/exams": ["Admin"],
  "/admin/testing": ["Admin"],
  "/admin/stipends": ["Admin"],
  "/schedule": ["Admin", "Dispatcher", "Teacher"],
}
