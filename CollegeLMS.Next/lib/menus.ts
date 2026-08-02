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
      { href: "/admin/specialties", label: "Специальности", icon: BadgeInfo },
      { href: "/admin/testing", label: "Тесты", icon: BookType },
    ],
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
  "/admin/specialties": ["Admin"],
  "/admin/testing": ["Admin"],
  "/schedule": ["Admin", "Dispatcher", "Teacher"],
}
