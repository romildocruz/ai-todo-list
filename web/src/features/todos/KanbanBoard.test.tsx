import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FakeApi, makeTodo, problem } from '../../test/fakeApi'
import { renderWithProviders } from '../../test/renderWithProviders'
import { KanbanBoard } from './KanbanBoard'

const todos = [
  makeTodo({ id: '1'.repeat(24), title: 'Estudar React', status: 'ToDo' }),
  makeTodo({ id: '2'.repeat(24), title: 'Revisar código', status: 'Doing' }),
  makeTodo({ id: '3'.repeat(24), title: 'Publicar versão', status: 'Done' }),
]

const column = (name: string) => screen.getByRole('region', { name })

describe('KanbanBoard', () => {
  afterEach(() => vi.unstubAllGlobals())

  describe('quadro', () => {
    it('distribui as atividades nas colunas conforme o status', async () => {
      new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)

      await screen.findByText('Estudar React')
      expect(within(column('A fazer')).getByText('Estudar React')).toBeInTheDocument()
      expect(within(column('Em andamento')).getByText('Revisar código')).toBeInTheDocument()
      expect(within(column('Concluído')).getByText('Publicar versão')).toBeInTheDocument()
    })
  })

  describe('estados de carregamento, erro e vazio', () => {
    it('mostra o indicador de carregamento durante a listagem', async () => {
      const api = new FakeApi(todos)
      let release!: () => void
      const gate = new Promise<void>((resolve) => (release = resolve))
      api.override = () => gate.then(() => undefined)
      renderWithProviders(<KanbanBoard />)

      expect(screen.getByRole('status')).toHaveTextContent('Carregando atividades...')

      release()
      expect(await screen.findByText('Estudar React')).toBeInTheDocument()
      expect(screen.queryByRole('status')).not.toBeInTheDocument()
    })

    it('mostra erro com "Tentar novamente" e recarrega ao clicar', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      api.override = ({ method }) => (method === 'GET' && api.calls.length === 1 ? problem(503) : undefined)
      renderWithProviders(<KanbanBoard />)

      expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível carregar as atividades.')

      await user.click(screen.getByRole('button', { name: 'Tentar novamente' }))

      expect(await screen.findByText('Estudar React')).toBeInTheDocument()
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('orienta a criar a primeira atividade quando não há nenhuma', async () => {
      new FakeApi([])
      renderWithProviders(<KanbanBoard />)

      expect(
        await screen.findByText('Nenhuma atividade ainda. Crie a primeira atividade para começar.'),
      ).toBeInTheDocument()
    })

    it('mostra "nenhuma atividade encontrada" quando a busca não tem resultados', async () => {
      const user = userEvent.setup()
      new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)
      await screen.findByText('Estudar React')

      await user.type(screen.getByLabelText('Buscar por título'), 'inexistente')

      expect(await screen.findByText('Nenhuma atividade encontrada.')).toBeInTheDocument()
    })
  })

  describe('busca e filtro', () => {
    it('envia a busca à API (com debounce) e exibe só os títulos correspondentes', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)
      await screen.findByText('Estudar React')

      await user.type(screen.getByLabelText('Buscar por título'), 'rev')

      await waitFor(() => expect(screen.queryByText('Estudar React')).not.toBeInTheDocument())
      expect(screen.getByText('Revisar código')).toBeInTheDocument()

      const searches = api.callsTo('GET').map((call) => call.query.get('search'))
      expect(searches).toEqual([null, 'rev']) // sem uma requisição por tecla digitada
    })

    it('filtra por status enviando o parâmetro à API', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)
      await screen.findByText('Estudar React')

      await user.click(screen.getByPlaceholderText('Todos os status'))
      await user.click(await screen.findByRole('option', { name: 'Em andamento' }))

      await waitFor(() => expect(screen.queryByText('Estudar React')).not.toBeInTheDocument())
      expect(screen.getByText('Revisar código')).toBeInTheDocument()
      expect(api.callsTo('GET').at(-1)?.query.get('status')).toBe('Doing')
    })
  })

  describe('alteração de status', () => {
    const moveToDone = async (user: ReturnType<typeof userEvent.setup>) => {
      const card = await screen.findByRole('article', { name: 'Estudar React' })
      await user.click(within(card).getByRole('button', { name: 'Mover para' }))
      await user.click(screen.getByRole('menuitem', { name: 'Concluído' }))
    }

    it('chama a API, move o cartão e notifica o sucesso', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)

      await moveToDone(user)

      expect(await within(column('Concluído')).findByText('Estudar React')).toBeInTheDocument()
      expect(within(column('A fazer')).queryByText('Estudar React')).not.toBeInTheDocument()
      expect(api.callsTo('PATCH')[0]).toMatchObject({ path: `/todos/${'1'.repeat(24)}/status`, body: { status: 'Done' } })
      expect(await screen.findByText('Status alterado com sucesso.')).toBeInTheDocument()
    })

    it('mantém o cartão na coluna original e notifica o erro quando a API falha', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      api.override = ({ method }) => (method === 'PATCH' ? problem(503) : undefined)
      renderWithProviders(<KanbanBoard />)

      await moveToDone(user)

      expect(await screen.findByText('Serviço temporariamente indisponível. Tente novamente em instantes.')).toBeInTheDocument()
      expect(within(column('A fazer')).getByText('Estudar React')).toBeInTheDocument()
      expect(within(column('Concluído')).queryByText('Estudar React')).not.toBeInTheDocument()
    })
  })

  describe('exclusão', () => {
    const askToDelete = async (user: ReturnType<typeof userEvent.setup>) => {
      const card = await screen.findByRole('article', { name: 'Estudar React' })
      await user.click(within(card).getByRole('button', { name: 'Excluir' }))
      return screen.findByRole('dialog', { name: 'Excluir atividade' })
    }

    it('só exclui depois da confirmação e notifica o sucesso', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)

      const dialog = await askToDelete(user)
      expect(api.callsTo('DELETE')).toHaveLength(0)

      await user.click(within(dialog).getByRole('button', { name: 'Excluir' }))

      await waitFor(() => expect(screen.queryByText('Estudar React')).not.toBeInTheDocument())
      expect(api.callsTo('DELETE')).toHaveLength(1)
      expect(await screen.findByText('Atividade excluída com sucesso.')).toBeInTheDocument()
    })

    it('não envia requisição e mantém a atividade quando cancelado', async () => {
      const user = userEvent.setup()
      const api = new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)

      const dialog = await askToDelete(user)
      await user.click(within(dialog).getByRole('button', { name: 'Cancelar' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(api.callsTo('DELETE')).toHaveLength(0)
      expect(screen.getByText('Estudar React')).toBeInTheDocument()
    })
  })

  describe('criação e edição pelo quadro', () => {
    it('cria uma atividade e a mostra na coluna "A fazer"', async () => {
      const user = userEvent.setup()
      new FakeApi([])
      renderWithProviders(<KanbanBoard />)
      await screen.findByText(/Crie a primeira atividade/)

      await user.click(screen.getByRole('button', { name: 'Nova atividade' }))
      await user.type(await screen.findByLabelText(/Título/), 'Nova tarefa')
      await user.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(await within(column('A fazer')).findByText('Nova tarefa')).toBeInTheDocument()
      expect(await screen.findByText('Atividade criada com sucesso.')).toBeInTheDocument()
    })

    it('abre a edição com os dados do cartão e atualiza o quadro', async () => {
      const user = userEvent.setup()
      new FakeApi(todos)
      renderWithProviders(<KanbanBoard />)

      const card = await screen.findByRole('article', { name: 'Estudar React' })
      await user.click(within(card).getByRole('button', { name: 'Editar' }))

      const title = await screen.findByLabelText(/Título/)
      expect(title).toHaveValue('Estudar React')
      await user.clear(title)
      await user.type(title, 'Estudar TypeScript')
      await user.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(await screen.findByText('Estudar TypeScript')).toBeInTheDocument()
      expect(screen.queryByText('Estudar React')).not.toBeInTheDocument()
    })
  })

  it('usa a resposta da API como única fonte: não toca no armazenamento local', async () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem')
    new FakeApi(todos)
    renderWithProviders(<KanbanBoard />)
    await screen.findByText('Estudar React')

    expect(setItem).not.toHaveBeenCalled()
    setItem.mockRestore()
  })
})
