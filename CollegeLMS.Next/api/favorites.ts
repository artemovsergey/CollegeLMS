import api from "@/lib/api"
import type { Result } from "@/types"

export type FavoriteTargetType = "Group" | "Teacher"

export interface Favorite {
  id: string
  targetType: FavoriteTargetType
  targetId: string
  groupName?: string | null
  teacherName?: string | null
}

export async function listFavorites(): Promise<Favorite[]> {
  const { data } = await api.get<Result<Favorite[]>>("/api/favorites")
  if (!data.isSuccess || !data.data)
    throw new Error(data.errorMessage ?? "Ошибка загрузки избранного")
  return data.data
}

export async function addFavorite(
  targetType: FavoriteTargetType,
  targetId: string,
): Promise<Favorite> {
  const { data } = await api.post<Result<Favorite>>("/api/favorites", {
    targetType,
    targetId,
  })
  if (!data.isSuccess || !data.data)
    throw new Error(data.errorMessage ?? "Ошибка добавления")
  return data.data
}

export async function removeFavorite(id: string): Promise<void> {
  const { data } = await api.delete<Result<null>>(`/api/favorites/${id}`)
  if (!data.isSuccess)
    throw new Error(data.errorMessage ?? "Ошибка удаления")
}