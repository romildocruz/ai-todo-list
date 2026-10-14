import { render, screen, within } from '@testing-library/react'
import { AppProviders } from '../../app/AppProviders'
import { makeTodo } from '../../test/fakeApi'
import { TodoColumn } from './TodoColumn'

const noop = () => {}

function renderColumn(todos = [makeTodo({ id: 'a'.repeat(24), title: 'Primeira' }), makeTodo({ id: 'b'.repeat(24), title: 'Segunda' })]) {
  render(
    <AppProviders env="test">
      <TodoColumn status="Doing" todos={todos} onEdit={noop} onDelete={noop} onChangeStatus={noop} />
    </AppProviders>,
  )
}

describe('TodoColumn', () => {
  it('mostra o nome da coluna, a contagem e os cartões', () => {
    renderColumn()

    const column = screen.getByRole('region', { name: 'Em andamento' })
    expect(within(column).getByLabelText('2 atividades')).toBeInTheDocument()
    expect(within(column).getByRole('article', { name: 'Primeira' })).toBeInTheDocument()
    expect(within(column).getByRole('article', { name: 'Segunda' })).toBeInTheDocument()
  })

  it('mostra mensagem amigável quando a coluna está vazia', () => {
    renderColumn([])

    expect(screen.getByText('Nenhuma atividade nesta coluna.')).toBeInTheDocument()
  })
})
