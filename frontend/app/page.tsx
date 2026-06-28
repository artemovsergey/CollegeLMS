"use client"

import { useEffect, useState } from "react"
import type { User, Result } from "@/types"

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users`)
      .then(r => r.json())
      .then((body: Result<User[]>) => {
        if (body.isSuccess && body.data) {
          setUsers(body.data)
        } else {
          setError(body.errorMessage ?? "Неизвестная ошибка")
        }
      })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <Loading />

  if (error) return <ErrorDisplay message={error} />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold">Пользователи</h1>

      {users.length === 0 ? (
        <p className="text-gray-500">Нет пользователей</p>
      ) : (
        <div className="overflow-x-auto rounded border border-gray-200">
          <table className="w-full text-left text-sm">
            <thead className="bg-gray-100">
              <tr>
                <th className="p-3 font-medium">Email</th>
                <th className="p-3 font-medium">ФИО</th>
                <th className="p-3 font-medium">Роль</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id} className="border-t border-gray-200 hover:bg-gray-50">
                  <td className="p-3">{u.email}</td>
                  <td className="p-3">{u.fullName}</td>
                  <td className="p-3">{roleLabel(u.role)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
    </div>
  )
}

function ErrorDisplay({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-2">
      <h2 className="text-xl font-semibold text-red-600">Ошибка</h2>
      <p className="text-gray-500">{message}</p>
    </div>
  )
}

function roleLabel(role: string): string {
  const labels: Record<string, string> = {
    Admin: "Админ",
    Teacher: "Преподаватель",
    Student: "Студент",
    Dispatcher: "Диспетчер",
  }
  return labels[role] ?? role
}
