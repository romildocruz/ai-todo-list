import type { Todo, TodoStatus } from '../api'

export function makeTodo(overrides: Partial<Todo> = {}): Todo {
  return {
    id: '6acf964fff4285ed0dc88831',
    title: 'Atividade',
    description: null,
    status: 'ToDo',
    createdAt: '2026-10-14T15:30:00.000Z',
    updatedAt: '2026-10-14T15:30:00.000Z',
    ...overrides,
  }
}

export interface RecordedCall {
  method: string
  path: string
  query: URLSearchParams
  body: unknown
}

type Reply = Response | undefined | Promise<Response | undefined>

/**
 * Substitui o `fetch` global por uma API de atividades em memória (sem rede), registrando as chamadas.
 * `override` permite forçar respostas específicas (erros, atrasos) por requisição.
 */
export class FakeApi {
  todos: Todo[]
  calls: RecordedCall[] = []
  override: ((call: RecordedCall) => Reply) | null = null

  constructor(todos: Todo[] = []) {
    this.todos = todos
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => this.handle(input, init)))
  }

  callsTo(method: string, pathPrefix = '/todos') {
    return this.calls.filter((call) => call.method === method && call.path.startsWith(pathPrefix))
  }

  private async handle(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
    const url = new URL(String(input), 'http://localhost')
    const call: RecordedCall = {
      method: init?.method ?? 'GET',
      path: url.pathname,
      query: url.searchParams,
      body: typeof init?.body === 'string' ? JSON.parse(init.body) : undefined,
    }
    this.calls.push(call)

    return (await this.override?.(call)) ?? this.route(call)
  }

  private route({ method, path, query, body }: RecordedCall): Response {
    const status = query.get('status') as TodoStatus | null
    const search = query.get('search')?.toLowerCase()
    const id = path.split('/')[2]
    const index = this.todos.findIndex((todo) => todo.id === id)
    const input = (body ?? {}) as Partial<Todo>

    if (method === 'GET' && path === '/todos') {
      return json(
        this.todos.filter(
          (todo) => (!status || todo.status === status) && (!search || todo.title.toLowerCase().includes(search)),
        ),
      )
    }
    if (method === 'POST' && path === '/todos') {
      const created = makeTodo({
        id: String(this.todos.length + 1).padStart(24, '0'),
        title: input.title ?? '',
        description: input.description ?? null,
      })
      this.todos = [created, ...this.todos]
      return json(created, 201)
    }
    if (index < 0) return problem(404)

    const current = this.todos[index]!
    if (method === 'PUT') return json(this.replace(index, { title: input.title, description: input.description ?? null }))
    if (method === 'PATCH') return json(this.replace(index, { status: input.status }))
    if (method === 'DELETE') {
      this.todos = this.todos.filter((todo) => todo.id !== current.id)
      return new Response(null, { status: 204 })
    }
    return problem(404)
  }

  private replace(index: number, changes: Partial<Todo>): Todo {
    const updated = { ...this.todos[index]!, ...changes }
    this.todos = this.todos.map((todo, i) => (i === index ? updated : todo))
    return updated
  }
}

export const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

export const problem = (status: number, extra: Record<string, unknown> = {}) =>
  json({ status, ...extra }, status)
