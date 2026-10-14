import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FakeApi, makeTodo, problem } from '../../test/fakeApi'
import { renderWithProviders } from '../../test/renderWithProviders'
import { TodoFormModal } from './TodoFormModal'
import { validateTodoForm } from './validateTodoForm'

describe('validateTodoForm', () => {
  it('rejeita título vazio, só com espaços ou acima de 100 caracteres', () => {
    expect(validateTodoForm({ title: '', description: '' }).title).toBe('O título é obrigatório.')
    expect(validateTodoForm({ title: '   ', description: '' }).title).toBe('O título é obrigatório.')
    expect(validateTodoForm({ title: 'a'.repeat(101), description: '' }).title).toBe(
      'O título deve ter no máximo 100 caracteres.',
    )
    expect(validateTodoForm({ title: 'a'.repeat(100), description: '' })).toEqual({})
  })

  it('rejeita descrição acima de 500 caracteres', () => {
    expect(validateTodoForm({ title: 'ok', description: 'a'.repeat(501) }).description).toBe(
      'A descrição deve ter no máximo 500 caracteres.',
    )
    expect(validateTodoForm({ title: 'ok', description: 'a'.repeat(500) })).toEqual({})
  })
})

describe('TodoFormModal', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('não envia a requisição e mostra o erro quando o título está vazio', async () => {
    const user = userEvent.setup()
    const api = new FakeApi()
    const onClose = vi.fn()
    renderWithProviders(<TodoFormModal opened todo={null} onClose={onClose} />)

    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(await screen.findByText('O título é obrigatório.')).toBeInTheDocument()
    expect(api.callsTo('POST')).toHaveLength(0)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('cria a atividade com título aparado, notifica o sucesso e fecha', async () => {
    const user = userEvent.setup()
    const api = new FakeApi()
    const onClose = vi.fn()
    renderWithProviders(<TodoFormModal opened todo={null} onClose={onClose} />)

    await user.type(screen.getByLabelText(/Título/), '  Comprar pão  ')
    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(onClose).toHaveBeenCalled())
    expect(api.callsTo('POST')[0]?.body).toEqual({ title: 'Comprar pão', description: null })
    expect(await screen.findByText('Atividade criada com sucesso.')).toBeInTheDocument()
  })

  it('exibe os erros da API junto aos campos e mantém o formulário aberto', async () => {
    const user = userEvent.setup()
    const api = new FakeApi()
    api.override = ({ method }) =>
      method === 'POST'
        ? problem(400, { errors: [{ name: 'title', reason: 'O título já existe.' }] })
        : undefined
    const onClose = vi.fn()
    renderWithProviders(<TodoFormModal opened todo={null} onClose={onClose} />)

    await user.type(screen.getByLabelText(/Título/), 'Repetida')
    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(await screen.findByText('O título já existe.')).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeEnabled()
  })

  it('edita preenchendo os dados atuais e envia PUT', async () => {
    const user = userEvent.setup()
    const todo = makeTodo({ title: 'Antigo', description: 'Detalhe' })
    const api = new FakeApi([todo])
    const onClose = vi.fn()
    renderWithProviders(<TodoFormModal opened todo={todo} onClose={onClose} />)

    const title = screen.getByLabelText(/Título/)
    expect(title).toHaveValue('Antigo')
    expect(screen.getByLabelText('Descrição')).toHaveValue('Detalhe')

    await user.clear(title)
    await user.type(title, 'Novo')
    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    await waitFor(() => expect(onClose).toHaveBeenCalled())
    expect(api.callsTo('PUT')[0]?.body).toEqual({ title: 'Novo', description: 'Detalhe' })
    expect(await screen.findByText('Atividade atualizada com sucesso.')).toBeInTheDocument()
  })
})
