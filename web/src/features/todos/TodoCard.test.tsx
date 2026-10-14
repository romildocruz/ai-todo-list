import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AppProviders } from '../../app/AppProviders'
import { makeTodo } from '../../test/fakeApi'
import { TodoCard } from './TodoCard'

function setup(todo = makeTodo({ title: 'Pagar contas', description: 'Luz e água' })) {
  const handlers = { onEdit: vi.fn(), onDelete: vi.fn(), onChangeStatus: vi.fn() }
  render(
    <AppProviders env="test">
      <TodoCard todo={todo} {...handlers} />
    </AppProviders>,
  )
  return { todo, ...handlers }
}

describe('TodoCard', () => {
  it('exibe título, descrição e data de criação no padrão pt-BR', () => {
    setup(makeTodo({ title: 'Pagar contas', description: 'Luz e água', createdAt: '2026-10-14T15:30:00.000Z' }))

    expect(screen.getByText('Pagar contas')).toBeInTheDocument()
    expect(screen.getByText('Luz e água')).toBeInTheDocument()
    expect(screen.getByText(/\d{2}\/\d{2}\/\d{4}/)).toBeInTheDocument()
  })

  it('exibe apenas título e data quando não há descrição', () => {
    setup(makeTodo({ title: 'Sem detalhes', description: null }))

    const card = screen.getByRole('article', { name: 'Sem detalhes' })
    expect(card.querySelectorAll('p')).toHaveLength(2) // título + data
  })

  it('oferece apenas os outros dois status no menu "Mover para"', async () => {
    const user = userEvent.setup()
    setup(makeTodo({ status: 'Doing' }))

    await user.click(screen.getByRole('button', { name: 'Mover para' }))

    expect(screen.getByRole('menuitem', { name: 'A fazer' })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: 'Concluído' })).toBeInTheDocument()
    expect(screen.queryByRole('menuitem', { name: 'Em andamento' })).not.toBeInTheDocument()
  })

  it('dispara os callbacks de status, edição e exclusão', async () => {
    const user = userEvent.setup()
    const { todo, onChangeStatus, onEdit, onDelete } = setup()

    await user.click(screen.getByRole('button', { name: 'Mover para' }))
    await user.click(screen.getByRole('menuitem', { name: 'Concluído' }))
    await user.click(screen.getByRole('button', { name: 'Editar' }))
    await user.click(screen.getByRole('button', { name: 'Excluir' }))

    expect(onChangeStatus).toHaveBeenCalledWith(todo, 'Done')
    expect(onEdit).toHaveBeenCalledWith(todo)
    expect(onDelete).toHaveBeenCalledWith(todo)
  })
})
