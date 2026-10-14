import { request } from './client'
import type { CreateTodoInput, ListTodosParams, Todo, TodoStatus, UpdateTodoInput } from './types'

export const todosApi = {
  list: (params: ListTodosParams = {}, signal?: AbortSignal) =>
    request<Todo[]>('/todos', { query: { status: params.status, search: params.search?.trim() }, signal }),

  getById: (id: string, signal?: AbortSignal) => request<Todo>(`/todos/${id}`, { signal }),

  create: (input: CreateTodoInput) => request<Todo>('/todos', { method: 'POST', body: input }),

  update: (id: string, input: UpdateTodoInput) => request<Todo>(`/todos/${id}`, { method: 'PUT', body: input }),

  changeStatus: (id: string, status: TodoStatus) =>
    request<Todo>(`/todos/${id}/status`, { method: 'PATCH', body: { status } }),

  remove: (id: string) => request<void>(`/todos/${id}`, { method: 'DELETE' }),
}
