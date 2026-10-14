export const TODO_STATUSES = ['ToDo', 'Doing', 'Done'] as const

export type TodoStatus = (typeof TODO_STATUSES)[number]

/** Atividade devolvida pela API. As datas são ISO 8601 em UTC. */
export interface Todo {
  id: string
  title: string
  description: string | null
  status: TodoStatus
  createdAt: string
  updatedAt: string
}

export interface CreateTodoInput {
  title: string
  description?: string | null
}

export interface UpdateTodoInput {
  title: string
  description?: string | null
}

export interface ListTodosParams {
  status?: TodoStatus
  search?: string
}

/** Erro de um campo, como devolvido em `errors` pela API (400). */
export interface FieldError {
  /** Nome do campo em camelCase (ex.: `title`). */
  name: string
  reason: string
}

/** Corpo Problem Details (RFC 9457) devolvido pela API em respostas de erro. */
export interface ProblemDetailsBody {
  title?: string
  detail?: string
  status?: number
  errors?: { name?: string; reason?: string }[]
}
