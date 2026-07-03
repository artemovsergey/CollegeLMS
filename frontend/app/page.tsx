"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { User, Result, CreateUserRequest, UpdateUserRequest, ChangeRoleRequest } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"

export default function UsersPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formEmail, setFormEmail] = useState("")
  const [formPassword, setFormPassword] = useState("")
  const [formFullName, setFormFullName] = useState("")
  const [formRole, setFormRole] = useState("Student")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const isAdmin = user?.role === "Admin"
  const roleLabels: Record<string, string> = {
    Admin: "Админ", Teacher: "Преподаватель", Student: "Студент", Dispatcher: "Диспетчер",
  }

  const fetchUsers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<User[]>>("/api/users")
      const body = res.data
      if (body.isSuccess && body.data) {
        setUsers(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки пользователей")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchUsers()
    }
  }, [token, fetchUsers])

  const resetForm = () => {
    setFormEmail("")
    setFormPassword("")
    setFormFullName("")
    setFormRole("Student")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateUserRequest = { email: formEmail, password: formPassword, fullName: formFullName, role: formRole }
      const res = await api.post<Result<User>>("/api/users", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchUsers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания пользователя")
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (u: User) => {
    setEditingId(u.id)
    setFormEmail(u.email)
    setFormFullName(u.fullName)
    setFormRole(u.role)
    setFormPassword("")
    setFormError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateUserRequest = { email: formEmail, fullName: formFullName, role: formRole }
      const res = await api.put<Result<User>>(`/api/users/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchUsers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления пользователя")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm("Деактивировать пользователя?")) return
    try {
      await api.delete(`/api/users/${id}`)
      await fetchUsers()
    } catch {
      setError("Ошибка деактивации")
    }
  }

  const handleChangeRole = async (id: string, role: string) => {
    try {
      const body: ChangeRoleRequest = { role }
      await api.patch(`/api/users/${id}/role`, body)
      await fetchUsers()
    } catch {
      setError("Ошибка смены роли")
    }
  }

  if (authLoading) return <Loading />
  if (!token) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">CollegeLMS</h1>
        <div className="flex items-center gap-3">
          <span className="text-sm text-gray-500">{user?.email}</span>
          <span className="text-xs rounded bg-blue-100 text-blue-800 px-2 py-0.5">{roleLabels[user?.role ?? ""] ?? user?.role}</span>
          <button onClick={() => { logout(); router.push("/login") }} className="text-sm text-red-600 hover:underline">
            Выйти
          </button>
        </div>
      </div>

      {/* Users section */}
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Пользователи</h2>
        {isAdmin && (
          <button onClick={() => { resetForm(); setShowCreate(true) }} className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 transition">
            + Создать
          </button>
        )}
      </div>

      {error && <p className="text-sm text-red-600 bg-red-50 rounded p-2">{error}</p>}

      {/* Create/Edit form */}
      {(showCreate || editingId) && (
        <form onSubmit={editingId ? handleUpdate : handleCreate} className="flex flex-col gap-3 p-4 rounded border border-gray-200 bg-white">
          <h3 className="font-medium">{editingId ? "Редактировать" : "Создать пользователя"}</h3>
          {formError && <p className="text-sm text-red-600">{formError}</p>}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <input placeholder="Email" type="email" required value={formEmail} onChange={e => setFormEmail(e.target.value)} className="rounded border border-gray-300 p-2 text-sm" />
            <input placeholder="Пароль" type="password" value={formPassword} onChange={e => setFormPassword(e.target.value)} className="rounded border border-gray-300 p-2 text-sm" required={!editingId} />
            <input placeholder="ФИО" required value={formFullName} onChange={e => setFormFullName(e.target.value)} className="rounded border border-gray-300 p-2 text-sm" />
            <select value={formRole} onChange={e => setFormRole(e.target.value)} className="rounded border border-gray-300 p-2 text-sm">
              {Object.entries(roleLabels).map(([key, label]) => (
                <option key={key} value={key}>{label}</option>
              ))}
            </select>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={formSubmitting} className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50 transition">
              {formSubmitting ? "Сохранение..." : "Сохранить"}
            </button>
            <button type="button" onClick={resetForm} className="rounded border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50 transition">
              Отмена
            </button>
          </div>
        </form>
      )}

      {/* Users table */}
      {loading ? (
        <Loading />
      ) : users.length === 0 ? (
        <p className="text-gray-500">Нет пользователей</p>
      ) : (
        <div className="overflow-x-auto rounded border border-gray-200">
          <table className="w-full text-left text-sm">
            <thead className="bg-gray-100">
              <tr>
                <th className="p-3 font-medium">Email</th>
                <th className="p-3 font-medium">ФИО</th>
                <th className="p-3 font-medium">Роль</th>
                <th className="p-3 font-medium">Статус</th>
                {isAdmin && <th className="p-3 font-medium">Действия</th>}
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id} className={`border-t border-gray-200 hover:bg-gray-50 ${!u.isActive ? "opacity-50" : ""}`}>
                  <td className="p-3">{u.email}</td>
                  <td className="p-3">{u.fullName}</td>
                  <td className="p-3">
                    {isAdmin ? (
                      <select
                        value={u.role}
                        onChange={e => handleChangeRole(u.id, e.target.value)}
                        className="rounded border border-gray-200 p-1 text-xs"
                      >
                        {Object.entries(roleLabels).map(([key, label]) => (
                          <option key={key} value={key}>{label}</option>
                        ))}
                      </select>
                    ) : (
                      roleLabels[u.role] ?? u.role
                    )}
                  </td>
                  <td className="p-3">
                    {u.isActive ? <span className="text-green-600">Активен</span> : <span className="text-gray-400">Неактивен</span>}
                  </td>
                  {isAdmin && (
                    <td className="p-3">
                      <div className="flex gap-2">
                        <button onClick={() => startEdit(u)} className="text-xs text-blue-600 hover:underline">Ред.</button>
                        {u.isActive && (
                          <button onClick={() => handleDelete(u.id)} className="text-xs text-red-600 hover:underline">Деакт.</button>
                        )}
                      </div>
                    </td>
                  )}
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
