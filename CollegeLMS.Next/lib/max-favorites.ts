export type FavoriteTargetType = "Group" | "Teacher"

export interface LocalFavorite {
  id: string
  targetType: FavoriteTargetType
  targetId: string
  name: string
}

const FAVORITES_KEY = "max-favorites"

export const FAVORITES_EVENT = "max:favorites"

function readAll(): LocalFavorite[] {
  if (typeof window === "undefined") return []
  try {
    const raw = localStorage.getItem(FAVORITES_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw) as LocalFavorite[]
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function notifyChanged(): void {
  if (typeof window === "undefined") return
  window.dispatchEvent(new Event(FAVORITES_EVENT))
}

function favoriteId(targetType: FavoriteTargetType, targetId: string): string {
  return `${targetType}:${targetId}`
}

export function listLocalFavorites(): LocalFavorite[] {
  return readAll()
}

export function isLocalFavorite(
  targetType: FavoriteTargetType,
  targetId: string,
): boolean {
  return readAll().some((f) => f.id === favoriteId(targetType, targetId))
}

export function addLocalFavorite(
  targetType: FavoriteTargetType,
  targetId: string,
  name: string,
): void {
  const all = readAll()
  const id = favoriteId(targetType, targetId)
  if (all.some((f) => f.id === id)) return
  all.push({ id, targetType, targetId, name })
  try {
    localStorage.setItem(FAVORITES_KEY, JSON.stringify(all))
  } catch {
    // приватный режим — избранное просто не сохранится
  }
  notifyChanged()
}

export function removeLocalFavorite(
  targetType: FavoriteTargetType,
  targetId: string,
): void {
  const id = favoriteId(targetType, targetId)
  const all = readAll().filter((f) => f.id !== id)
  try {
    if (all.length === 0) localStorage.removeItem(FAVORITES_KEY)
    else localStorage.setItem(FAVORITES_KEY, JSON.stringify(all))
  } catch {
    // приватный режим
  }
  notifyChanged()
}