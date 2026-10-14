import { messages } from '../messages'
import type { FieldError, ProblemDetailsBody } from './types'

/** Falha de uma chamada à API, já com mensagem em pt-BR pronta para exibir. */
export class ApiError extends Error {
  /** Código HTTP, ou `null` quando não houve resposta (falha de rede). */
  readonly status: number | null
  /** Erros por campo (somente em 400 de validação). */
  readonly fieldErrors: FieldError[]

  constructor(message: string, status: number | null, fieldErrors: FieldError[] = []) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }

  /** Primeiro erro de cada campo, para alimentar formulários. */
  get fieldErrorMap(): Record<string, string> {
    const map: Record<string, string> = {}
    for (const { name, reason } of this.fieldErrors) {
      map[name] ??= reason
    }
    return map
  }
}

export function getApiBaseUrl(): string {
  const url = import.meta.env.VITE_API_BASE_URL as string | undefined
  return (url ?? '').replace(/\/+$/, '')
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  query?: Record<string, string | undefined>
  body?: unknown
  signal?: AbortSignal
}

/** `fetch` tipado: devolve o JSON da resposta (ou `undefined` em 204) e converte falhas em `ApiError`. */
export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', query, body, signal } = options

  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value) params.set(key, value)
  }
  const queryString = params.toString()
  const url = `${getApiBaseUrl()}${path}${queryString ? `?${queryString}` : ''}`

  let response: Response
  try {
    response = await fetch(url, {
      method,
      signal,
      headers: {
        Accept: 'application/json, application/problem+json',
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(messages.errors.network, null)
  }

  if (response.ok) {
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  throw await toApiError(response)
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = await readProblem(response)
  const fieldErrors = (problem?.errors ?? []).flatMap(({ name, reason }) =>
    name && reason ? [{ name: toCamelCase(name), reason }] : [],
  )

  return new ApiError(errorMessage(response.status, problem, fieldErrors), response.status, fieldErrors)
}

async function readProblem(response: Response): Promise<ProblemDetailsBody | null> {
  try {
    const parsed: unknown = await response.json()
    return typeof parsed === 'object' && parsed !== null ? (parsed as ProblemDetailsBody) : null
  } catch {
    return null
  }
}

function errorMessage(status: number, problem: ProblemDetailsBody | null, fieldErrors: FieldError[]): string {
  // A API devolve as mensagens em pt-BR: usa a do primeiro campo, ou o detalhe do problema.
  const fromApi = fieldErrors[0]?.reason ?? problem?.detail
  if (fromApi) return fromApi

  switch (status) {
    case 400:
      return messages.errors.invalid
    case 404:
      return messages.errors.notFound
    case 503:
      return messages.errors.unavailable
    default:
      return messages.errors.unexpected
  }
}

function toCamelCase(name: string): string {
  return name.charAt(0).toLowerCase() + name.slice(1)
}
