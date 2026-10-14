import { ApiError } from './client'
import { todosApi } from './todosApi'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

describe('cliente da API', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.test/api/')
  })

  afterEach(() => {
    vi.unstubAllEnvs()
    vi.unstubAllGlobals()
  })

  it('lista atividades enviando apenas os filtros informados', async () => {
    const fetchMock = vi.fn().mockResolvedValue(json([]))
    vi.stubGlobal('fetch', fetchMock)

    await todosApi.list({ status: 'Doing', search: '  casa ' })

    expect(fetchMock.mock.calls[0]?.[0]).toBe('http://api.test/api/todos?status=Doing&search=casa')
  })

  it('trata 204 como sucesso sem corpo', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))

    await expect(todosApi.remove('6acf964fff4285ed0dc88831')).resolves.toBeUndefined()
  })

  it('converte 400 em ApiError com erros por campo em camelCase', async () => {
    const problem = { errors: [{ name: 'Title', reason: 'O título é obrigatório.' }] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(problem, 400)))

    const error = await todosApi.create({ title: '' }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(400)
    expect((error as ApiError).fieldErrorMap).toEqual({ title: 'O título é obrigatório.' })
  })

  it('usa a mensagem em pt-BR para 404 sem detalhe', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 404 })))

    await expect(todosApi.getById('6acf964fff4285ed0dc88831')).rejects.toMatchObject({
      status: 404,
      message: 'Atividade não encontrada.',
    })
  })

  it('converte falha de rede em ApiError sem status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(todosApi.list()).rejects.toMatchObject({ status: null })
  })
})
